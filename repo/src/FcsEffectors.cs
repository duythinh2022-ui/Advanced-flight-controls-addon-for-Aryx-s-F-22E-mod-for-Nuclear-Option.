// F-22E FCS — effector models and the stall-aware nonlinear control allocator.
//
// Every effector is modelled with the SAME equations Nuclear Option's AeroJob_Math uses, evaluated in
// managed code against the live transforms:
//   local velocity   vloc = inverse(R_lift(δ)) * v_part
//   job angle        a    = atan2(vloc.y, vloc.z)          (radians; conventional local AoA = -a)
//   coefficients     CL,CD = airfoil chart(a)              (same 128-point chart the job builds)
//   force            F    = -normalize(v x right(δ)) * CL*q*S*eff  -  v̂ * CD*q*S*eff
//   point            p    = part.rb.worldCenterOfMass + R_lift(δ)*centerOfLift
//   moment about CG  M    = (p - CG) x F
// The surface transform is rotated about its own local X axis by the servo (ControlSurfaceJob_Math), so
// R_lift(δ) is reconstructed exactly from the visible-mesh transform. Because each candidate deflection is
// evaluated on the real lift curve at the surface's OWN local AoA, the allocator never drives a surface
// past its C_L peak and, on a surface that is already post-stall, it deflects the "reversed" way
// automatically — that is the reversal logic, done by model rather than by a hand-tuned table.
using System;
using System.Collections.Generic;
using UnityEngine;
using NuclearOption.Jobs;

namespace Aryx_F22E_StrikeRaptor.FCS
{
    internal sealed class AirfoilTable
    {
        private readonly float[] cl = new float[128];
        private readonly float[] cd = new float[128];
        private readonly bool generic;

        public AirfoilTable(Airfoil af)
        {
            if (af == null || af.liftCoef == null || af.dragCoef == null) { generic = true; return; }
            for (int i = 0; i < 128; i++)
            {
                float a = (i - 64) * 0.04908734f;
                cl[i] = af.liftCoef.Evaluate(a);
                cd[i] = af.dragCoef.Evaluate(a);
            }
        }

        private static float Read(float[] t, float idx)
        {
            if (idx <= 0f) return t[0];
            if (idx >= 127f) return t[127];
            int i0 = (int)idx; float f = idx - i0;
            return t[i0] + (t[i0 + 1] - t[i0]) * f;
        }

        /// <param name="a">job angle in radians (atan2(vloc.y, vloc.z))</param>
        public void Get(float a, out float CL, out float CD)
        {
            if (generic)
            {
                CL = 1.8f * Mathf.Sin(5f * a);
                CD = 1.5f * (1f - Mathf.Cos(2f * a)) + 0.02f;
                return;
            }
            float idx = a * 20.37185f + 64f;
            CL = Read(cl, idx);
            CD = Read(cd, idx);
        }

        /// <summary>Local AoA (deg, conventional sign) of this airfoil's positive C_L peak.</summary>
        public float StallAoADeg()
        {
            if (generic) return 18f;
            int best = 64; for (int i = 64; i < 96; i++) if (cl[i] > cl[best]) best = i;
            return (best - 64) * 0.04908734f * Mathf.Rad2Deg;
        }
    }

    /// <summary>Per-step shared state for effector evaluation.</summary>
    internal struct EvalContext
    {
        public Vector3 cg;               // world CG
        public Quaternion rootInv;       // inverse of aircraft root rotation
        public float rho;                // air density
        public Vector3 wind;             // world wind velocity
    }

    internal abstract class Effector
    {
        public string name;
        public bool active;
        public float min, max;           // command range (deg)
        public float now;                // actual (servo) position (deg)
        public float cmd;                // current command (deg)
        public float cmdOut;             // command sent to the servo (after actuator damping)
        public float cmdLag;             // actuator damping pre-filter state
        public RateTraj outTraj;         // actuator damping: acceleration-limited servo command (full servo speed kept)
        public float rate;               // actuator rate (deg/s)
        public float mu = 1.0e-4f;       // preference for neutral (cost per full-travel^2)
        public float lam = 2.0e-5f;      // cost of moving command (per full-travel^2)
        public float lo, hi;             // usable box this step (<= min/max; e.g. rudder fade at high AoA)
        // Pairing (left/right surfaces): u = pairSign * d. anti = (u_a + u_b)/2 is the surface pair's
        // control-axis use (roll for flaperons/stabs, yaw for rudders), sym = (u_a - u_b)/2 the other one.
        public Effector partner;         // set on the leader of a pair only
        public float pairSign = 1f;
        public float symCost, antiCost;
        public int N;
        public float[] grid;
        public Vector3[] M;              // aero-axis moments (roll, pitch, yaw) at grid points
        public Vector3 Mnow;             // moment at 'now'

        protected void AllocGrid(int n)
        {
            N = n; grid = new float[n]; M = new Vector3[n];
        }

        public float Step => (max - min) / (N - 1);

        public Vector3 MAt(float c)
        {
            if (N < 2) return Vector3.zero;
            float x = (c - min) / Step;
            if (x <= 0f) return M[0];
            if (x >= N - 1) return M[N - 1];
            int i = (int)x; float f = x - i;
            return M[i] + (M[i + 1] - M[i]) * f;
        }

        public abstract void Evaluate(ref EvalContext ctx);

        protected static Vector3 ToAero(Quaternion rootInv, Vector3 worldMoment)
        {
            Vector3 l = rootInv * worldMoment;
            return new Vector3(-l.z, -l.x, l.y); // (roll right, pitch up, yaw right)
        }
    }

    internal enum SurfaceKind { Stabilator, Flaperon, Aileron, Rudder, Other }

    internal sealed class SurfaceEffector : Effector
    {
        public readonly ControlSurface cs;
        public readonly AeroPart part;
        public readonly SurfaceKind kind;
        public readonly AirfoilTable foil;
        public readonly float stallAoA;
        public float origPitchRange, origRollRange, origYawRange;
        public bool overriding;
        public float localAoANow;        // deg, conventional (+ = lifting in the surface's +lift sense)
        public float localAoACmd;        // deg, at the commanded deflection
        // Stabilators: the pitch axis sees a linear, never-reversed model (no stall protection / no reversal use in
        // pitch); roll and yaw keep the full stall-aware model. pitchFrac = remaining pitch authority (0 post-stall).
        public bool linearPitch, noPitch;
        public float pitchFrac = 1f, pitchSlope, localAoA0;
        public float[] DbgRaw;   // bench: raw pitch-moment table before the monotone fix
        public bool revFlow;
        internal static float RevFlowOn = 120f, RevFlowOff = 110f;
        internal static bool RevFlowEnabled = true;   // bench switch

        public SurfaceEffector(ControlSurface cs, AeroPart part, SurfaceKind kind, AirfoilTable foil)
        {
            this.cs = cs; this.part = part; this.kind = kind; this.foil = foil;
            name = cs.gameObject.name.Replace("Aryx_KingRaptor_", "");
            origPitchRange = Acc.CS_pitchRange(cs); origRollRange = Acc.CS_rollRange(cs); origYawRange = Acc.CS_yawRange(cs);
            float travel = Mathf.Abs(origPitchRange) + Mathf.Abs(origRollRange) + Mathf.Abs(origYawRange);
            if (travel < 1f) travel = 20f;
            min = -travel; max = travel; lo = min; hi = max;
            stallAoA = foil.StallAoADeg();
            int n = Mathf.Clamp(Mathf.RoundToInt(2f * travel / 2f) + 1, 9, 31); // ~2 deg spacing
            AllocGrid(n);
            for (int i = 0; i < n; i++) grid[i] = min + i * (max - min) / (n - 1);
        }

        public bool Valid =>
            cs != null && part != null && Acc.CS_visible(cs) != null && Acc.AP_liftNormal(part) != null &&
            part.rb != null && Acc.JobCreated(cs) && !part.IsDetached();

        // Transform cache for the current step
        private Quaternion visRot, lRel;
        private float deltaVis;
        private Vector3 com, col, v, vhat;
        private float qS, v2;

        public override void Evaluate(ref EvalContext ctx)
        {
            active = false;
            if (!Valid) return;
            ref ControlSurfaceFields f = ref Acc.Job(cs);
            if (f.IsDetached) return;

            Transform vis = Acc.CS_visible(cs).transform;
            Transform lt = Acc.AP_liftNormal(part);
            visRot = vis.rotation;
            lRel = Quaternion.Inverse(visRot) * lt.rotation;
            // deflection currently shown by the visible transform relative to its resting pose
            Quaternion dq = Quaternion.Inverse(f.restingRotation) * vis.localRotation;
            deltaVis = 2f * Mathf.Atan2(dq.x, dq.w) * Mathf.Rad2Deg;
            if (deltaVis > 180f) deltaVis -= 360f; else if (deltaVis < -180f) deltaVis += 360f;
            now = f.currentPitch + f.currentRoll + f.currentYaw;
            rate = Mathf.Max(1f, f.servoSpeed);

            com = part.rb.worldCenterOfMass;
            col = Acc.AP_centerOfLift(part);
            v = part.rb.velocity - ctx.wind;
            v2 = v.sqrMagnitude;
            if (v2 < 1f) { v2 = 0f; vhat = Vector3.forward; }
            else vhat = v / Mathf.Sqrt(v2);
            qS = 0.5f * ctx.rho * v2 * Acc.AP_wingArea(part) * Acc.AP_wingEff(part);

            for (int i = 0; i < N; i++) M[i] = Eval(grid[i], ref ctx, out _);
            Mnow = Eval(now, ref ctx, out localAoANow);
            if (noPitch)
            {
                // roll/yaw-only surfaces: their pitch side-effect is left to the INDI loop instead of being used
                // (no flaperon-vs-aileron "fighting" pairs that make pitch out of their stall nonlinearity)
                for (int i = 0; i < N; i++) M[i].y = Mnow.y;
            }
            if (linearPitch)
            {
                // Pitch axis without stall exploitation: the pitch moment is made monotone in the normal direction
                // (+deflection = nose up). Wherever the real curve reverses (surface post-stall), the modelled
                // moment is held flat, so the allocator never deflects a stalled stab "backwards" for pitch and
                // never holds it deflected for a moment it does not normally make. Roll/yaw keep the real curve.
                float up = -Mathf.Sign(origPitchRange == 0f ? -1f : origPitchRange);
                // Reversed flow (tail slide: the stab's own local AoA past ~120 deg, air arriving over the trailing
                // edge): the stab still has pitch authority, but in the opposite deflection sense. Holding the
                // model to the normal direction there flattened it to zero, so the stabs sat still while the tail
                // slide diverged nose-down. With hysteresis (on above 120, off below 110) the monotone model follows
                // the reversed direction instead. The 60..110 deg band (deep stall, little usable moment) keeps the
                // normal-direction clamp.
                float la0 = Mathf.Abs(LocalAoAAt(0f));
                if (!revFlow && la0 > RevFlowOn) revFlow = true; else if (revFlow && la0 < RevFlowOff) revFlow = false;
                if (revFlow && RevFlowEnabled) up = -up;
                if (DbgRaw != null && DbgRaw.Length >= N) for (int i = 0; i < N; i++) DbgRaw[i] = M[i].y;
                float tMin = float.MaxValue, tMax = float.MinValue;
                for (int i = 0; i < N; i++) { tMin = Mathf.Min(tMin, M[i].y); tMax = Mathf.Max(tMax, M[i].y); }
                if (up > 0f) { for (int i = 1; i < N; i++) M[i].y = Mathf.Max(M[i].y, M[i - 1].y); }
                else { for (int i = N - 2; i >= 0; i--) M[i].y = Mathf.Max(M[i].y, M[i + 1].y); }
                // keep the table consistent with the true moment at the present position (INDI increment base)
                float off = Mnow.y - MAt(now).y;
                for (int i = 0; i < N; i++) M[i].y += off;
                float mMin = float.MaxValue, mMax = float.MinValue;
                for (int i = 0; i < N; i++) { mMin = Mathf.Min(mMin, M[i].y); mMax = Mathf.Max(mMax, M[i].y); }
                // usable (normal-direction) share of the stab's pitch-moment range: 1 attached, 0 fully post-stall
                float raw = tMax - tMin;
                pitchFrac = raw > 1e-3f ? Mathf.Clamp01(((mMax - mMin) - 0.25f * raw) / (0.75f * raw)) : 1f;
                localAoA0 = LocalAoAAt(0f);
            }
            active = true;
        }

        public Vector3 Eval(float delta, ref EvalContext ctx, out float localAoADeg)
        {
            Quaternion R = visRot * Quaternion.AngleAxis(delta - deltaVis, Vector3.right) * lRel;
            Vector3 vloc = Quaternion.Inverse(R) * v;
            float a = Mathf.Atan2(vloc.y, vloc.z);
            localAoADeg = -a * Mathf.Rad2Deg;
            if (v2 <= 0f) return Vector3.zero;
            foil.Get(a, out float CL, out float CD);
            Vector3 right = R * Vector3.right;
            Vector3 n = Vector3.Cross(v, right);
            float nm = n.magnitude;
            if (nm < 1e-4f) return Vector3.zero;
            n /= nm;
            Vector3 F = -n * (CL * qS) - vhat * (CD * qS);
            Vector3 p = com + R * col;
            return ToAero(ctx.rootInv, Vector3.Cross(p - ctx.cg, F));
        }

        public float LocalAoAAt(float delta)
        {
            Quaternion R = visRot * Quaternion.AngleAxis(delta - deltaVis, Vector3.right) * lRel;
            Vector3 vloc = Quaternion.Inverse(R) * v;
            return -Mathf.Atan2(vloc.y, vloc.z) * Mathf.Rad2Deg;
        }
    }

    /// <summary>Pitch-only thrust vectoring, both engines slaved to one nozzle angle.</summary>
    internal sealed class TvcEffector : Effector
    {
        public readonly List<Turbofan> engines = new List<Turbofan>();
        public float totalThrust;

        public TvcEffector(IEnumerable<Turbofan> fans)
        {
            name = "TVC";
            engines.AddRange(fans);
            float t = 10f;
            foreach (var e in engines) t = Mathf.Max(0.1f, Mathf.Abs(Acc.TF_vec(e).x));
            min = -t; max = t; lo = min; hi = max;
            // Trim is shared with the stabilators (F-22 practice: nozzle trim reduces trim drag and keeps the
            // stabs near neutral); dynamic demand still goes mostly to the faster aero surfaces via lam.
            mu = 5.0e-5f;
            lam = 2.0e-5f;
            AllocGrid(11);
            for (int i = 0; i < N; i++) grid[i] = min + i * (max - min) / (N - 1);
        }

        public float NozzleLimit => max;

        public override void Evaluate(ref EvalContext ctx)
        {
            active = false;
            for (int i = 0; i < N; i++) M[i] = Vector3.zero;
            Mnow = Vector3.zero;
            totalThrust = 0f;
            bool any = false;
            float nowSum = 0f; int cnt = 0;
            foreach (var e in engines)
            {
                if (e == null || !Acc.TF_operable(e)) continue;
                Transform[] vts = Acc.TF_vecT(e);
                if (vts == null || vts.Length == 0) continue;
                Aircraft eac = Acc.TF_aircraft(e);
                if (eac != null && eac.speed > Acc.TF_vecMaxSpeed(e)) continue;
                Transform vec = vts[0];
                if (vec == null) continue;
                Quaternion parentRot = vec.parent != null ? vec.parent.rotation : Quaternion.identity;
                Vector3 nzAng = Acc.TF_nozzleAngles(e);
                float yawNow = nzAng.y;
                nowSum += nzAng.x; cnt++;
                JetNozzle[] nozzles = Acc.TF_nozzles(e);
                if (nozzles == null) continue;
                foreach (var nz in nozzles)
                {
                    Transform thrT = nz != null ? Acc.JN_thrustT(nz) : null;
                    if (thrT == null) continue;
                    float T = Acc.JN_totalThrust(nz);
                    if (T <= 1f) continue;
                    totalThrust += T;
                    Quaternion vecRot = vec.rotation;
                    Quaternion rel = Quaternion.Inverse(vecRot) * thrT.rotation;
                    Vector3 posRel = Quaternion.Inverse(vecRot) * (thrT.position - vec.position);
                    for (int i = 0; i <= N; i++)
                    {
                        float th = i < N ? grid[i] : nzAng.x;
                        Quaternion Rv = parentRot * Quaternion.Euler(Mathf.Clamp(th, -20f, 20f), yawNow, 0f);
                        Vector3 dir = Rv * rel * Vector3.forward;
                        Vector3 pos = vec.position + Rv * posRel;
                        Vector3 m = ToAero(ctx.rootInv, Vector3.Cross(pos - ctx.cg, dir * T));
                        if (i < N) M[i] += m; else Mnow += m;
                    }
                    any = true;
                }
            }
            now = cnt > 0 ? nowSum / cnt : 0f;
            rate = 70f * Plugin.ActuatorScale;
            active = any;
        }
    }

    /// <summary>
    /// Weighted nonlinear least-squares control allocator.
    ///   cost = |W * I^-1 * (sum_i M_i(d_i) - M_target)|^2 + sum_i mu_i (d_i/D_i)^2 + lam_i ((d_i - d_prev_i)/D_i)^2
    /// Stage 1: damped Gauss-Newton on all effectors jointly (slopes from each effector's sampled moment curve
    ///          at its current command, box-constrained) - distributes the demand by effectiveness and cost.
    ///          On a post-stall surface the local slope is reversed, so the joint step deflects it the
    ///          reversed way; near the C_L peak the slope vanishes and the surface stops being pushed.
    /// Stage 2: one exhaustive 1-D sweep per effector over its whole travel, so a surface can jump across
    ///          its stall peak when that is globally better than creeping along a local slope.
    /// </summary>
    internal sealed class Allocator
    {
        public Vector3 axisWeight = new Vector3(1.0f, 1.5f, 1.0f); // roll, pitch, yaw
        public int gnIterations = 4;
        public static double LmFactor = 0.001;      // Levenberg damping (fraction of each effector's own curvature)

        private float i00, i01, i02, i10, i11, i12, i20, i21, i22;
        private readonly List<Effector> act = new List<Effector>();
        private double[] x = new double[0], prev = new double[0], dx = new double[0];
        private double[,] H = new double[0, 0];
        private double[] rhs = new double[0];
        private Vector3[] Jc = new Vector3[0];

        public void SetInverseInertia(float[,] inv)
        {
            i00 = inv[0, 0]; i01 = inv[0, 1]; i02 = inv[0, 2];
            i10 = inv[1, 0]; i11 = inv[1, 1]; i12 = inv[1, 2];
            i20 = inv[2, 0]; i21 = inv[2, 1]; i22 = inv[2, 2];
        }

        // weighted angular-acceleration error (rad/s^2 * sqrt(weight))
        private Vector3 E(Vector3 m)
        {
            float a = i00 * m.x + i01 * m.y + i02 * m.z;
            float b = i10 * m.x + i11 * m.y + i12 * m.z;
            float c = i20 * m.x + i21 * m.y + i22 * m.z;
            return new Vector3(a * Mathf.Sqrt(axisWeight.x), b * Mathf.Sqrt(axisWeight.y), c * Mathf.Sqrt(axisWeight.z));
        }

        private float Cost(Vector3 err) => E(err).sqrMagnitude;

        private static float Reg(Effector e, float c, float p)
        {
            float span = Mathf.Max(1f, e.max);
            float n = c / span, d = (c - p) / span;
            return e.mu * n * n + e.lam * d * d;
        }

        private int[] pidx = new int[0];   // index of the pair partner in act[] (leader only), else -1

        private float PairReg(int i)
        {
            int k = pidx[i]; if (k < 0) return 0f;
            Effector a = act[i], b = act[k];
            float span = Mathf.Max(1f, a.max);
            float ua = a.pairSign * (float)x[i] / span, ub = b.pairSign * (float)x[k] / span;
            float sym = 0.5f * (ua - ub), anti = 0.5f * (ua + ub);
            return a.symCost * sym * sym + a.antiCost * anti * anti;
        }

        // pair cost with effector j's value replaced by c (for the 1-D sweep)
        private float PairRegWith(int j, float c)
        {
            double keep = x[j]; x[j] = c;
            float r = 0f;
            for (int i = 0; i < act.Count; i++) if (pidx[i] >= 0 && (i == j || pidx[i] == j)) r += PairReg(i);
            x[j] = keep;
            return r;
        }

        private float Total(Vector3 target)
        {
            Vector3 S = Vector3.zero; float r = 0f;
            for (int i = 0; i < act.Count; i++) { S += act[i].MAt((float)x[i]); r += Reg(act[i], (float)x[i], (float)prev[i]) + PairReg(i); }
            return Cost(S - target) + r;
        }

        private void Ensure(int n)
        {
            if (x.Length == n) return;
            x = new double[n]; prev = new double[n]; dx = new double[n]; H = new double[n, n]; rhs = new double[n]; Jc = new Vector3[n]; pidx = new int[n];
        }

        public void Solve(List<Effector> effs, Vector3 target)
        {
            act.Clear();
            foreach (var e in effs) if (e.active) act.Add(e);
            int n = act.Count;
            if (n == 0) return;
            Ensure(n);
            for (int i = 0; i < n; i++) { x[i] = Mathf.Clamp(act[i].cmd, act[i].lo, act[i].hi); prev[i] = act[i].cmd; }
            for (int i = 0; i < n; i++) pidx[i] = act[i].partner != null ? act.IndexOf(act[i].partner) : -1;

            float J0 = Total(target);
            for (int it = 0; it < gnIterations; it++)
            {
                Vector3 S = Vector3.zero;
                for (int i = 0; i < n; i++) S += act[i].MAt((float)x[i]);
                Vector3 e0 = E(S - target);
                for (int i = 0; i < n; i++)
                {
                    Effector ef = act[i];
                    float h = 0.5f * ef.Step;
                    float a = Mathf.Max(ef.min, (float)x[i] - h), b = Mathf.Min(ef.max, (float)x[i] + h);
                    Jc[i] = b > a ? E(ef.MAt(b) - ef.MAt(a)) / (b - a) : Vector3.zero;
                }
                for (int i = 0; i < n; i++)
                {
                    Effector ef = act[i];
                    float span = Mathf.Max(1f, ef.max); double s2 = 1.0 / (span * span);
                    for (int k = 0; k < n; k++) H[i, k] = Vector3.Dot(Jc[i], Jc[k]);
                    double lm = LmFactor * H[i, i] + 1e-9;
                    H[i, i] += (ef.mu + ef.lam) * s2 + lm;
                    rhs[i] = -(Vector3.Dot(Jc[i], e0) + ef.mu * s2 * x[i] + ef.lam * s2 * (x[i] - prev[i]));
                }
                // pair costs: c_s/4 (s_a x_a - s_b x_b)^2 + c_a/4 (s_a x_a + s_b x_b)^2, in units of 1/span^2
                for (int i = 0; i < n; i++)
                {
                    int k = pidx[i]; if (k < 0) continue;
                    Effector a = act[i], b = act[k];
                    float span = Mathf.Max(1f, a.max); double s2 = 1.0 / (span * span);
                    double sa = a.pairSign, sb = b.pairSign;
                    double cs = 0.25 * a.symCost * s2, ca = 0.25 * a.antiCost * s2;
                    double us = sa * x[i] - sb * x[k], ua = sa * x[i] + sb * x[k];
                    H[i, i] += cs + ca; H[k, k] += cs + ca;
                    H[i, k] += (-cs + ca) * sa * sb; H[k, i] += (-cs + ca) * sa * sb;
                    rhs[i] -= cs * sa * us + ca * sa * ua;
                    rhs[k] -= -cs * sb * us + ca * sb * ua;
                }
                if (!SolveLinear(H, rhs, dx, n)) break;
                // project onto the box; line search on the true nonlinear cost
                double step = 1.0; bool accepted = false;
                double[] xs = new double[n];
                for (int ls = 0; ls < 4 && !accepted; ls++, step *= 0.5)
                {
                    for (int i = 0; i < n; i++) xs[i] = x[i];
                    for (int i = 0; i < n; i++) x[i] = Mathf.Clamp((float)(xs[i] + step * dx[i]), act[i].lo, act[i].hi);
                    float J1 = Total(target);
                    if (J1 < J0) { J0 = J1; accepted = true; }
                    else for (int i = 0; i < n; i++) x[i] = xs[i];
                }
                if (!accepted) break;
            }

            // Stage 2: exhaustive 1-D refinement over each effector's full travel
            Vector3 Ssum = Vector3.zero;
            for (int i = 0; i < n; i++) Ssum += act[i].MAt((float)x[i]);
            for (int i = 0; i < n; i++)
            {
                Effector e = act[i];
                float cur = (float)x[i], p = (float)prev[i];
                Vector3 rest = Ssum - e.MAt(cur);
                Vector3 need = target - rest;
                float bestJ = Cost(e.MAt(cur) - need) + Reg(e, cur, p) + PairRegWith(i, cur), bestC = cur; int bestK = -1;
                for (int k = 0; k < e.N; k++)
                {
                    float g = e.grid[k];
                    if (g < e.lo - 1e-4f || g > e.hi + 1e-4f) continue;
                    float Jk = Cost(e.M[k] - need) + Reg(e, g, p) + PairRegWith(i, g);
                    if (Jk < bestJ - 1e-9f) { bestJ = Jk; bestC = g; bestK = k; }
                }
                if (bestK > 0 && bestK < e.N - 1)
                {
                    float ga = e.grid[bestK - 1], gc = e.grid[bestK + 1];
                    float ja = Cost(e.M[bestK - 1] - need) + Reg(e, ga, p) + PairRegWith(i, ga);
                    float jc = Cost(e.M[bestK + 1] - need) + Reg(e, gc, p) + PairRegWith(i, gc);
                    float den = ja - 2f * bestJ + jc;
                    if (den > 1e-12f)
                    {
                        float c = Mathf.Clamp(e.grid[bestK] + Mathf.Clamp(0.5f * (ja - jc) / den, -0.5f, 0.5f) * e.Step, e.lo, e.hi);
                        float Jr = Cost(e.MAt(c) - need) + Reg(e, c, p) + PairRegWith(i, c);
                        if (Jr < bestJ) { bestJ = Jr; bestC = c; }
                    }
                }
                x[i] = bestC;
                Ssum = rest + e.MAt(bestC);
            }

            for (int i = 0; i < n; i++) act[i].cmd = Mathf.Clamp((float)x[i], act[i].lo, act[i].hi);
        }

        // Gaussian elimination with partial pivoting (n <= ~12)
        private static bool SolveLinear(double[,] A0, double[] b0, double[] outX, int n)
        {
            double[,] A = (double[,])A0.Clone(); double[] b = (double[])b0.Clone();
            for (int c = 0; c < n; c++)
            {
                int piv = c; double best = Math.Abs(A[c, c]);
                for (int r = c + 1; r < n; r++) if (Math.Abs(A[r, c]) > best) { best = Math.Abs(A[r, c]); piv = r; }
                if (best < 1e-18) return false;
                if (piv != c)
                {
                    for (int k = 0; k < n; k++) { double t = A[c, k]; A[c, k] = A[piv, k]; A[piv, k] = t; }
                    double tb = b[c]; b[c] = b[piv]; b[piv] = tb;
                }
                for (int r = c + 1; r < n; r++)
                {
                    double f = A[r, c] / A[c, c];
                    if (f == 0) continue;
                    for (int k = c; k < n; k++) A[r, k] -= f * A[c, k];
                    b[r] -= f * b[c];
                }
            }
            for (int r = n - 1; r >= 0; r--)
            {
                double s = b[r];
                for (int k = r + 1; k < n; k++) s -= A[r, k] * outX[k];
                outX[r] = s / A[r, r];
            }
            return true;
        }
    }
}
