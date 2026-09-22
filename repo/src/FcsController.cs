// F-22E FCS — control laws.
//
// Sign conventions used everywhere in this file (aero body axes, right-handed):
//   alpha  > 0 nose up relative to the flight path      beta > 0 wind from the right (nose left of path)
//   p      > 0 roll right       q > 0 pitch up          r    > 0 yaw right       (deg/s in the laws)
//   stick  sp > 0 pull          sr > 0 right            sy   > 0 right pedal
// Nuclear Option's ControlInputs use pitch +1 = nose DOWN (stick forward), roll +1 = right, yaw +1 = right
// (derived from the stock ControlsFilter/FlyByWire rate targets). Unity angular velocity maps to aero as
//   p = -w.z   q = -w.x   r = +w.y      (local frame of the aircraft root)
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Aryx_F22E_StrikeRaptor.FCS
{
    internal enum FcsMode { Off, Ground, GearDownRate, AoAG, MPO }

    internal struct Lpf
    {
        public float y; public bool init;
        public float Step(float x, float tau, float dt)
        {
            if (!init) { y = x; init = true; return y; }
            y += (x - y) * (dt / (tau + dt));
            return y;
        }
    }

    internal struct Lpf3
    {
        public Vector3 y; public bool init;
        public Vector3 Step(Vector3 x, float tau, float dt)
        {
            if (!init) { y = x; init = true; return y; }
            y += (x - y) * (dt / (tau + dt));
            return y;
        }
    }

    /// <summary>Jerk-limited rate trajectory: x = commanded rate, v = its derivative (commanded angular
    /// acceleration, fed forward to the inner loop). Accelerates at up to A, changes acceleration at up to J,
    /// and brakes on the sqrt(2 J |e|) curve so it arrives with zero acceleration (no overshoot).</summary>
    internal struct RateTraj
    {
        public float x, v;
        public void Reset(float x0) { x = x0; v = 0f; }
        public void Step(float target, float A, float J, float dt)
        {
            float e = target - x;
            // target moved back past us while we were still accelerating away from it: stop moving away
            // (otherwise the command would run on beyond its limit until the jerk limit caught up)
            if (e * v < 0f) v = 0f;
            // braking curve for a discrete-time jerk limit (v shrinks by J*dt per step and reaches zero exactly
            // as e does; the continuous sqrt(2 J e) curve ends with a one-step acceleration snap)
            float jd = J * dt;
            float vDes = Mathf.Sign(e) * Mathf.Min(A, Mathf.Sqrt(0.25f * jd * jd + 2f * J * Mathf.Abs(e)) - 0.5f * jd);
            v = Mathf.MoveTowards(v, vDes, J * dt);
            float nx = x + v * dt;
            if ((target - nx) * e <= 0f) { nx = target; v = 0f; }
            x = nx;
        }
    }

    internal sealed class FcsController
    {
        public readonly Aircraft ac;
        public readonly ControlInputs inputs;
        public readonly List<SurfaceEffector> surfaces = new List<SurfaceEffector>();
        public readonly TvcEffector tvc;
        public readonly FlapSystem flaps;
        private readonly List<Effector> effs = new List<Effector>();
        private readonly Allocator alloc = new Allocator();
        private readonly List<LandingGear> gears = new List<LandingGear>();
        private readonly List<Rigidbody> bodies = new List<Rigidbody>();
        private readonly List<AeroPart> aeroParts = new List<AeroPart>();
        private readonly Dictionary<AeroPart, AirfoilTable> foils = new Dictionary<AeroPart, AirfoilTable>();

        public bool Engaged;
        public bool Failed;
        public FcsMode Mode = FcsMode.Off;

        // mass properties
        private float mass = 20000f;
        private Vector3 cg;
        private readonly float[,] Ia = new float[3, 3];
        private readonly float[,] IaInv = new float[3, 3];
        private float massTimer = 99f, nAlphaTimer = 99f;

        // measured state
        private Vector3 omega, omegaPrev;             // aero rad/s
        private Lpf3 omegaDotF, mNowF, accF;
        private Vector3 velPrev;
        private bool havePrev;
        private Lpf alphaDotF, aUpF, aDnF, qTdotF;
        private float alphaPrev;
        public float alphaU;                          // AoA unwrapped through +-180 (protection side)
        public bool protHold; private float protHoldSide;                        // protection override latched: zero pitch rate while full aft/forward stick is held
        public float alpha, beta, tas, ias, nz, omegaV, eLy, phi, theta, alphaDot, nAlpha = 0.1f, tw;
        public float nMaxAero = 9f, alphaAtNMax = 30f, gAvail = 9f, gInt, vDot, alphaDotHoldG;
        private Lpf vDotF;
        public float aUp = 60f, aDn = 60f;           // available pitch accel (deg/s^2) up / down from now
        internal static float RollLoadLead = 0.2f;
        internal static float RollStickRate = 10f;
        internal static float PreLag = 0.01f;
        internal static bool LegacyLag;             // bench only: 1.6.2 first-order actuator lag for comparison
        internal static float RollJerkServo = 1f;    // roll-trajectory jerk <= this x servo-limited roll-accel slew
        private float rollSlew = 100f;               // 1/s: slowest roll surface half-travel per second   // s of extra load-limiter lead at full roll rate
        public static bool DebugYaw;             // bench only: per-group yaw moment breakdown
        public float dbgWVe, dbgYawRud, dbgYawWing, dbgYawStab, dbgYawTvc, dbgTargetZ, dbgMfZ, dbgTgtY, dbgMfY;
        public float aRoll = 300f, aYaw = 60f;      // available roll / yaw accel magnitude (deg/s^2)
        private Lpf aRollF, aYawF, aRollWF;
        public float aRollWing = 200f;               // roll accel of the wing surfaces alone (flaperons + outer flaperons), deg/s^2
        public float pS;                             // issued stability-axis roll rate (deg/s)
        private RateTraj pTraj, qTraj;
        private Lpf rSF;
        public float rudderAuth = 1f;
        public float aInt;                          // AoA-command integrator (deg/s)
        public float yawSat; private Lpf yawSatF, guardF;
        public float stabPitchFrac = 1f;
        private float pCmdPrev, rCmdPrev; private Lpf pDotF, rDotF;
        public float pFull = 300f, pStockRef, pMaxDbg, rMaxDbg, rollCtrlHalf = 1e5f;

        // pilot
        public float sp, sr, sy; private float spF, srF, syF;

        // pitch law state
        public float qB, qRaw, qCmd, qT, qTprev, betaG, alphaCmd, nCmd;
        public bool unload, prot;
        public float qMaxDbg, qMinDbg;
        public float pCmd, rCmd, pDotFF, rDotFF;
        public Vector3 omegaDotDes, mTarget;

        // TVC output consumed by the Turbofan transpiler (ControlInputs.pitch-equivalent units)
        public float tvcPitchInput;

        private int groundFrames;
        public bool rotating, mainsOnGround;
        private float noseDownTime;
        private LandingGear noseGearCached; private bool noseGearSearched;
        /// <summary>The gear leg furthest ahead of the others (the nose wheel); null if the layout is not tricycle.</summary>
        private LandingGear NoseGear()
        {
            if (noseGearSearched) return noseGearCached;
            noseGearSearched = true;
            LandingGear best = null; float bz = float.MinValue, second = float.MinValue; int n = 0;
            foreach (LandingGear g in gears)
            {
                if (g == null) continue;
                n++;
                float z = Vector3.Dot(g.transform.position - (ac.rb != null ? ac.rb.worldCenterOfMass : ac.transform.position), ac.transform.forward);
                if (z > bz) { second = bz; bz = z; best = g; } else if (z > second) second = z;
            }
            noseGearCached = n >= 2 && bz - second > 1.5f ? best : null;
            Plugin.Log.LogInfo(noseGearCached != null ? $"F-22E FCS: nose gear '{noseGearCached.name}' ({bz - second:F1} m ahead of the next leg), {n} gear legs." : $"F-22E FCS: no distinct nose gear among {n} gear legs; rotation law disabled.");
            return noseGearCached;
        }
        private FcsMode lastLoggedMode = FcsMode.Off;
        private Telemetry telemetry;
        private float time;

        public FcsController(Aircraft aircraft)
        {
            ac = aircraft;
            inputs = aircraft.GetInputs();
            AircraftParameters prm = aircraft.GetAircraftParameters();

            var css = Acc.Collect<ControlSurface>(aircraft);
            foreach (ControlSurface cs in css)
            {
                AeroPart part = Acc.CS_attached(cs) as AeroPart;
                if (part == null) continue;
                SurfaceKind kind = Classify(cs);
                surfaces.Add(new SurfaceEffector(cs, part, kind, FoilFor(part, prm)));
            }
            if (surfaces.Count < 6)
                throw new InvalidOperationException("F-22E FCS: expected 8 control surfaces, found " + surfaces.Count);

            // Rudders are the primary yaw effector; bias the stabilators slightly against differential use
            // so roll comes from the flaperons/ailerons first (the F-22 uses differential stab as a
            // secondary roll effector).
            foreach (var s in surfaces)
            {
                if (s.kind == SurfaceKind.Stabilator) { s.mu = 2.0e-4f; s.linearPitch = true; }
                else s.noPitch = true;
                effs.Add(s);
            }
            // Left/right pairs. Flaperons, outer flaperons and rudders are used antisymmetrically (roll / yaw):
            // symmetric use is expensive, so they are never the pitch trimmer (the flap schedule owns their
            // symmetric position). Stabilators are the pitch pair; differential (roll) use costs a little so
            // roll comes from the wing first and pitch keeps the stabs.
            foreach (var l in surfaces)
            {
                if (!l.name.EndsWith("_L")) continue;
                var r = surfaces.Find(o => o.kind == l.kind && o.name == l.name.Substring(0, l.name.Length - 2) + "_R");
                if (r == null) continue;
                bool rud = l.kind == SurfaceKind.Rudder;
                float sl = rud ? Mathf.Sign(l.origYawRange) : Mathf.Sign(l.origRollRange);
                float sr = rud ? Mathf.Sign(r.origYawRange) : Mathf.Sign(r.origRollRange);
                if (sl == 0f || sr == 0f || sl == sr) continue;
                l.partner = r; l.pairSign = sl; r.pairSign = sr;
                // stabs: symmetric (pitch) use costs more than the nozzles, so steady trim / sustained pitch rate
                // is carried by TVC and the stabs are used for the transients TVC cannot follow
                if (l.kind == SurfaceKind.Stabilator) { l.symCost = Plugin.StabTrimCost; l.antiCost = 1.0e-2f; }
                else { l.symCost = 2.0f; l.antiCost = 0f; }
            }

            tvc = new TvcEffector(Acc.Collect<Turbofan>(aircraft));
            effs.Insert(0, tvc);

            gears.AddRange(Acc.Collect<LandingGear>(aircraft));
            foreach (UnitPart up in aircraft.partLookup)
            {
                if (up is AeroPart ap) { aeroParts.Add(ap); if (!foils.ContainsKey(ap)) foils[ap] = FoilFor(ap, prm); }
            }
            RebuildBodies();
            flaps = new FlapSystem(aircraft, surfaces);
            if (Plugin.TelemetryEnabled) telemetry = new Telemetry(this);

            Plugin.Log.LogInfo($"F-22E FCS attached: {surfaces.Count} surfaces [{string.Join(", ", surfaces.ConvertAll(s => s.name + ":" + s.kind + " ±" + s.max.ToString("0")))}], " +
                               $"TVC engines {tvc.engines.Count} (±{tvc.max:0} deg pitch only), {bodies.Count} rigidbodies, {flaps.Describe()}.");
        }

        private static SurfaceKind Classify(ControlSurface cs)
        {
            string n = cs.gameObject.name;
            if (Mathf.Abs(Acc.CS_pitchRange(cs)) > 0.01f || n.Contains("Elevator") || n.Contains("Stab")) return SurfaceKind.Stabilator;
            if (n.Contains("Rudder")) return SurfaceKind.Rudder;
            if (n.Contains("Flap")) return SurfaceKind.Flaperon;
            if (n.Contains("Aileron")) return SurfaceKind.Aileron;
            if (Mathf.Abs(Acc.CS_yawRange(cs)) > Mathf.Abs(Acc.CS_rollRange(cs))) return SurfaceKind.Rudder;
            return SurfaceKind.Other;
        }

        private static AirfoilTable FoilFor(AeroPart part, AircraftParameters prm)
        {
            Airfoil af = null;
            int ai = Acc.AP_airfoil(part);
            if (ai >= 0 && prm != null && prm.airfoils != null && ai < prm.airfoils.Length)
                af = prm.airfoils[ai];
            return new AirfoilTable(af);
        }

        private void RebuildBodies()
        {
            bodies.Clear();
            foreach (UnitPart up in ac.partLookup)
            {
                if (up == null || up.rb == null || up.IsDetached()) continue;
                if (!bodies.Contains(up.rb)) bodies.Add(up.rb);
            }
            if (bodies.Count == 0 && ac.rb != null) bodies.Add(ac.rb);
        }

        public void Dispose()
        {
            telemetry?.Close();
            telemetry = null;
        }

        // ------------------------------------------------------------------------------------------
        // Mass properties: composite CG and inertia tensor of all attached part rigidbodies
        // ------------------------------------------------------------------------------------------
        private void UpdateMassProps(bool full, Quaternion rootInv)
        {
            float m = 0f; Vector3 s = Vector3.zero;
            for (int i = bodies.Count - 1; i >= 0; i--)
            {
                Rigidbody b = bodies[i];
                if (b == null) { bodies.RemoveAt(i); continue; }
                m += b.mass; s += b.mass * b.worldCenterOfMass;
            }
            if (m < 1f) return;
            mass = m; cg = s / m;
            if (!full) return;

            double[,] Iw = new double[3, 3];
            foreach (Rigidbody b in bodies)
            {
                Matrix4x4 R = Matrix4x4.Rotate(b.rotation * b.inertiaTensorRotation);
                Vector3 d = b.inertiaTensor;
                Vector3 r = b.worldCenterOfMass - cg;
                float r2 = r.sqrMagnitude;
                for (int i = 0; i < 3; i++)
                    for (int j = 0; j < 3; j++)
                    {
                        double v = R[i, 0] * d.x * R[j, 0] + R[i, 1] * d.y * R[j, 1] + R[i, 2] * d.z * R[j, 2];
                        v += b.mass * ((i == j ? r2 : 0f) - r[i] * r[j]);
                        Iw[i, j] += v;
                    }
            }
            // world -> root local -> aero.  P maps Unity-local (x right, y up, z fwd) pseudovectors to
            // (roll, pitch, yaw) = (-z, -x, +y).
            Matrix4x4 Q = Matrix4x4.Rotate(rootInv);
            double[,] Il = new double[3, 3];
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    double acc = 0;
                    for (int k = 0; k < 3; k++)
                        for (int l = 0; l < 3; l++)
                            acc += Q[i, k] * Iw[k, l] * Q[j, l];
                    Il[i, j] = acc;
                }
            int[] idx = { 2, 0, 1 };
            float[] sgn = { -1f, -1f, 1f };
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    Ia[i, j] = (float)(sgn[i] * sgn[j] * Il[idx[i], idx[j]]);
            Invert3(Ia, IaInv);
            alloc.SetInverseInertia(IaInv);
        }

        private static void Invert3(float[,] a, float[,] o)
        {
            float det = a[0, 0] * (a[1, 1] * a[2, 2] - a[1, 2] * a[2, 1])
                      - a[0, 1] * (a[1, 0] * a[2, 2] - a[1, 2] * a[2, 0])
                      + a[0, 2] * (a[1, 0] * a[2, 1] - a[1, 1] * a[2, 0]);
            if (Mathf.Abs(det) < 1e-6f) { for (int i = 0; i < 3; i++) for (int j = 0; j < 3; j++) o[i, j] = i == j ? 1f / Mathf.Max(1f, a[i, i]) : 0f; return; }
            float id = 1f / det;
            o[0, 0] = (a[1, 1] * a[2, 2] - a[1, 2] * a[2, 1]) * id;
            o[0, 1] = (a[0, 2] * a[2, 1] - a[0, 1] * a[2, 2]) * id;
            o[0, 2] = (a[0, 1] * a[1, 2] - a[0, 2] * a[1, 1]) * id;
            o[1, 0] = (a[1, 2] * a[2, 0] - a[1, 0] * a[2, 2]) * id;
            o[1, 1] = (a[0, 0] * a[2, 2] - a[0, 2] * a[2, 0]) * id;
            o[1, 2] = (a[0, 2] * a[1, 0] - a[0, 0] * a[1, 2]) * id;
            o[2, 0] = (a[1, 0] * a[2, 1] - a[1, 1] * a[2, 0]) * id;
            o[2, 1] = (a[0, 1] * a[2, 0] - a[0, 0] * a[2, 1]) * id;
            o[2, 2] = (a[0, 0] * a[1, 1] - a[0, 1] * a[1, 0]) * id;
        }

        private Vector3 MulI(Vector3 v) => new Vector3(
            Ia[0, 0] * v.x + Ia[0, 1] * v.y + Ia[0, 2] * v.z,
            Ia[1, 0] * v.x + Ia[1, 1] * v.y + Ia[1, 2] * v.z,
            Ia[2, 0] * v.x + Ia[2, 1] * v.y + Ia[2, 2] * v.z);

        // ------------------------------------------------------------------------------------------
        // Airframe lift slope n_alpha (g per deg of AoA), evaluated on the game's own aero model for
        // every aero part: used to turn a G-margin into an AoA-margin for the kinematic G limiter.
        // ------------------------------------------------------------------------------------------
        private static readonly float[] envAlphas = { -1f, 0f, 1f, 5f, 10f, 15f, 20f, 25f, 30f, 35f, 40f, 45f, 50f, 55f, 60f, 65f };
        private readonly float[] envLift = new float[16];

        /// <summary>
        /// Sweeps the whole airframe (every aero part, game aero equations, current dynamic pressure and
        /// surface positions) through AoA 0..65 deg. Gives n_alpha at the current AoA and the maximum
        /// load factor the airframe can actually generate at this speed (aero + thrust normal component).
        /// </summary>
        public float Lp = -1e5f;                     // roll damping, aero roll moment per rad/s of body roll (< 0)
        private void ComputeEnvelope(Vector3 wind, float rho, Vector3 rootRight, Vector3 eL)
        {
            for (int k = 0; k < envLift.Length; k++) envLift[k] = 0f;
            Quaternion rInv = Quaternion.Inverse(ac.transform.rotation);
            Vector3 dW = -ac.transform.forward * 0.2f;       // +0.2 rad/s of aero roll
            float L0 = 0f, L1 = 0f;
            float W = mass * 9.81f;
            foreach (AeroPart p in aeroParts)
            {
                if (p == null || p.rb == null) continue;
                Transform ln = Acc.AP_liftNormal(p);
                float S = Acc.AP_wingArea(p);
                if (ln == null || p.IsDetached() || S <= 0f) continue;
                if (!foils.TryGetValue(p, out AirfoilTable t)) continue;
                Vector3 v = p.rb.velocity - wind;
                float v2 = v.sqrMagnitude; if (v2 < 25f) continue;
                float qS = 0.5f * rho * v2 * S * Acc.AP_wingEff(p);
                Quaternion lr = ln.rotation;
                Vector3 right = lr * Vector3.right;
                // roll damping: same part with the velocity field of a small body roll rate added
                Vector3 arm = p.rb.worldCenterOfMass + lr * Acc.AP_centerOfLift(p) - cg;
                Vector3 v1 = v + Vector3.Cross(dW, p.rb.worldCenterOfMass - cg);
                L0 += -(rInv * Vector3.Cross(arm, PartForce(v, lr, right, t, 0.5f * rho * S * Acc.AP_wingEff(p)))).z;
                L1 += -(rInv * Vector3.Cross(arm, PartForce(v1, lr, right, t, 0.5f * rho * S * Acc.AP_wingEff(p)))).z;
                for (int k = 0; k < envAlphas.Length; k++)
                {
                    float da = k < 3 ? envAlphas[k] : envAlphas[k] - alpha;   // first three: -1/0/+1 about current
                    Quaternion rot = Quaternion.AngleAxis(da, rootRight);
                    envLift[k] += PartLift(rot * v, lr, right, t, qS, eL);
                }
            }
            nAlpha = Mathf.Clamp((envLift[2] - envLift[0]) * 0.5f / W, 0.005f, 12f);
            Lp = Mathf.Min((L1 - L0) / 0.2f, -1f);
            float best = float.MinValue, bestA = 30f;
            for (int k = 3; k < envAlphas.Length; k++)
            {
                float n = envLift[k] / W + tw * Mathf.Sin(envAlphas[k] * Mathf.Deg2Rad);
                if (n > best) { best = n; bestA = envAlphas[k]; }
            }
            nMaxAero = Mathf.Max(1f, best);
            alphaAtNMax = bestA;
        }

        /// <summary>Full aero force of one part (lift + airfoil drag), game equations. qSr = 0.5 rho S eff.</summary>
        private static Vector3 PartForce(Vector3 v, Quaternion lr, Vector3 right, AirfoilTable t, float qSr)
        {
            float v2 = v.sqrMagnitude; if (v2 < 1f) return Vector3.zero;
            Vector3 vloc = Quaternion.Inverse(lr) * v;
            t.Get(Mathf.Atan2(vloc.y, vloc.z), out float CL, out float CD);
            Vector3 n = Vector3.Cross(v, right); float nm = n.magnitude; if (nm < 1e-4f) return Vector3.zero;
            float qS = qSr * v2;
            return -n / nm * (CL * qS) - v / Mathf.Sqrt(v2) * (CD * qS);
        }

        /// <summary>
        /// Roll-rate ceiling. Reference = what the ORIGINAL aircraft achieves: stock FlyByWire roll law
        /// (target 286 deg/s = maxRollAngularVel 10 x clamp 0.5; P gain 1.1 blended toward direct stick by
        /// q/q_corner, corner 140 m/s) on the stock airframe with roll TVC. From a pure-roll run of that law on
        /// this airframe's aero, the stock steady rate is p = kV per unit roll input with k = 0.01868 rad/s per
        /// m/s (full-deflection helix), and the law's equilibrium gives p = kV(1+4.5r)/(1+1.1 r k V),
        /// r = 1/max(1, q/q_corner). Scaled by 1.2 at low IAS and 1.1 at high IAS (blend around corner speed).
        /// </summary>
        private float StockRollRef()
        {
            float kV = 0.01868f * tas;
            float loc1 = tas * tas * Mathf.Max(0.01f, ac.airDensity) / (140f * 140f * 1.225f);
            float r = 1f / Mathf.Max(1f, loc1);
            float p = Mathf.Min(kV, kV * (1f + 4.5f * r) / (1f + 1.1f * r * kV)) * Mathf.Rad2Deg;
            float f = Mathf.Lerp(Plugin.RollGainLowIAS, Plugin.RollGainHighIAS, Mathf.InverseLerp(120f, 170f, ias));
            return p * f;
        }

        /// <summary>Flight-path load factor the airframe would produce at AoA a (deg) at the present speed:
        /// envelope-sweep lift + thrust normal component. Linear through the lift slope outside 0..65 deg.</summary>
        private float NPath(float a)
        {
            // envelope entries 3..15 are absolute AoA 5..65 deg (entries 0..2 are -1/0/+1 about the present AoA)
            float W = mass * 9.81f;
            float Nk(int k) => envLift[k] / W + tw * Mathf.Sin(envAlphas[k] * Mathf.Deg2Rad);
            if (a <= 5f) return Nk(3) + nAlpha * (a - 5f);
            if (a >= 65f) return Nk(15);
            float x = (a - 5f) / 5f; int i = Mathf.Clamp((int)x, 0, 11); float f = x - i;
            return Nk(3 + i) + (Nk(4 + i) - Nk(3 + i)) * f;
        }

        private static float PartLift(Vector3 v, Quaternion lr, Vector3 right, AirfoilTable t, float qS, Vector3 eL)
        {
            Vector3 vloc = Quaternion.Inverse(lr) * v;
            float a = Mathf.Atan2(vloc.y, vloc.z);
            t.Get(a, out float CL, out _);
            Vector3 n = Vector3.Cross(v, right); float nm = n.magnitude; if (nm < 1e-4f) return 0f;
            return Vector3.Dot(-n / nm * (CL * qS), eL);
        }

        // ------------------------------------------------------------------------------------------
        public void Tick(ControlInputs inp, bool flightAssist)
        {
            float dt = Mathf.Max(1e-3f, Time.fixedDeltaTime);
            time += dt;
            Rigidbody rb = ac.rb;
            Transform root = ac.transform;
            if (rb == null || root == null) return;

            Quaternion rootRot = root.rotation, rootInv = Quaternion.Inverse(rootRot);
            massTimer += dt;
            bool fullMass = massTimer > 0.5f;
            if (fullMass) { massTimer = 0f; RebuildBodies(); }
            UpdateMassProps(fullMass || !havePrev, rootInv);

            // ---------------- kinematics ----------------
            Vector3 wind = Acc.AC_wind(ac);
            Vector3 vW = rb.velocity - wind;
            tas = vW.magnitude;
            float rho = Mathf.Max(0.01f, ac.airDensity);
            ias = tas * Mathf.Sqrt(rho / 1.225f);
            Vector3 vL = rootInv * vW;
            alpha = tas > 1f ? Mathf.Atan2(-vL.y, vL.z) * Mathf.Rad2Deg : 0f;
            beta = tas > 1f ? Mathf.Asin(Mathf.Clamp(vL.x / tas, -1f, 1f)) * Mathf.Rad2Deg : 0f;
            Vector3 wU = rootInv * rb.angularVelocity;
            omega = new Vector3(-wU.z, -wU.x, wU.y);

            Vector3 aW = havePrev ? (rb.velocity - velPrev) / dt : Vector3.zero;
            Vector3 aF = accF.Step(aW, 0.05f, dt);
            Vector3 f = aF - Physics.gravity;
            nz = Vector3.Dot(f, root.up) / 9.81f;
            Vector3 eL = tas > 1f ? Vector3.Cross(vW, root.right).normalized : root.up;
            eLy = eL.y;
            float Vc = Mathf.Max(tas, 30f);
            omegaV = Vector3.Dot(aF, eL) / Mathf.Max(tas, 5f) * Mathf.Rad2Deg;
            vDot = vDotF.Step(tas > 1f ? Vector3.Dot(aF, vW / tas) : 0f, 0.2f, dt);
            theta = Mathf.Asin(Mathf.Clamp(root.forward.y, -1f, 1f)) * Mathf.Rad2Deg;
            phi = Mathf.Atan2(-root.right.y, root.up.y) * Mathf.Rad2Deg;
            float aRaw = havePrev ? Mathf.DeltaAngle(alphaPrev, alpha) / dt : 0f;
            alphaDot = alphaDotF.Step(aRaw, 0.05f, dt);
            alphaPrev = alpha;
            // AoA unwrapped through +-180 (tail slide): the protection keeps the side it was on instead of flipping
            // from +179 to -179 (which read as a -45 deg limit violation and shoved the nose the other way).
            // Back in the ordinary range it is the plain AoA again.
            alphaU = Mathf.Abs(alpha) < 90f || !havePrev ? alpha : Mathf.Clamp(alphaU + Mathf.DeltaAngle(alphaU, alpha), -300f, 300f);

            // ---------------- flaps (moves lift normals: before the effector models) ----------------
            bool wowNow = false;
            foreach (LandingGear g in gears) if (g != null && g.WeightOnWheel(0.05f)) { wowNow = true; break; }
            flaps.Update(dt, this, wowNow && tas < 110f, ac.speed, Mathf.Clamp(inp.roll, -1f, 1f));

            // rudders fade out at high AoA (vertical-tail blanking on the real F-22; in this airframe model the
            // canted rudders' roll grows against their falling yaw power)
            rudderAuth = 1f - Mathf.Clamp01((Mathf.Abs(alpha) - Plugin.RudderFadeStart) / Mathf.Max(1f, Plugin.RudderFadeEnd - Plugin.RudderFadeStart));
            // tail slide: past ~120 deg AoA the fins sit in clean reversed flow again, so they come back (the
            // allocator's full model already carries the reversed sign) to keep the jet from swapping ends in yaw
            if (RudderRevBack) rudderAuth = Mathf.Max(rudderAuth, Mathf.InverseLerp(120f, 140f, Mathf.Abs(alpha)));
            foreach (var s in surfaces)
            {
                float k = s.kind == SurfaceKind.Rudder ? rudderAuth : 1f;
                s.lo = s.min * k; s.hi = s.max * k;
            }

            // ---------------- effector models ----------------
            EvalContext ctx = new EvalContext { cg = cg, rootInv = rootInv, rho = rho, wind = wind };
            Vector3 mNow = Vector3.zero;
            foreach (Effector e in effs)
            {
                e.Evaluate(ref ctx);
                if (e.active) { mNow += e.Mnow; if (!Engaged || !havePrev) { e.cmd = e.now; e.cmdOut = e.now; e.cmdLag = e.now; e.outTraj.Reset(e.now); } }
            }
            Vector3 wdRaw = havePrev ? (omega - omegaPrev) / dt : Vector3.zero;
            const float tauF = 0.04f; // INDI sync filter
            Vector3 wdF = omegaDotF.Step(wdRaw, tauF, dt);
            Vector3 mF = mNowF.Step(mNow, tauF, dt);
            omegaPrev = omega; velPrev = rb.velocity; havePrev = true;

            // capability (pitch) from the sampled effector curves
            float mUp = 0f, mDn = 0f; Vector3 capPos = Vector3.zero, capNeg = Vector3.zero;
            foreach (Effector e in effs)
            {
                if (!e.active) continue;
                Vector3 hi = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                Vector3 lo = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                for (int k = 0; k < e.N; k++) { hi = Vector3.Max(hi, e.M[k]); lo = Vector3.Min(lo, e.M[k]); }
                capPos += hi; capNeg += lo;
            }
            mUp = capPos.y; mDn = capNeg.y;
            float iyyInv = IaInv[1, 1];
            aUp = Mathf.Clamp(aUpF.Step((wdF.y + (mUp - mF.y) * iyyInv) * Mathf.Rad2Deg, 0.15f, dt), 5f, 1500f);
            aDn = Mathf.Clamp(aDnF.Step(-(wdF.y + (mDn - mF.y) * iyyInv) * Mathf.Rad2Deg, 0.15f, dt), 5f, 1500f);
            // symmetric roll / yaw authority (half the full-travel moment swing)
            // Usable roll authority: what the roll-preferred effectors (flaperons, outer flaperons) can do plus a
            // quarter of the differential stabs (their roll use is costed, they belong to pitch first). The
            // full-travel sum over every effector overstated it ~2x, so the roll command planned on accelerations
            // only full deflection could give.
            float rollHalf = 0f, rollHalfWing = 0f; rollSlew = 100f;
            foreach (var sf in surfaces)
            {
                if (!sf.active) continue;
                float w = sf.kind == SurfaceKind.Flaperon || sf.kind == SurfaceKind.Aileron ? 1f : sf.kind == SurfaceKind.Stabilator ? Mathf.Lerp(Plugin.StabRollShare, 0.25f, Mathf.InverseLerp(160f, 240f, ias)) : 0f;
                if (w == 0f) continue;
                float hi = float.MinValue, lo = float.MaxValue;
                for (int k = 0; k < sf.N; k++) { hi = Mathf.Max(hi, sf.M[k].x); lo = Mathf.Min(lo, sf.M[k].x); }
                rollHalf += w * 0.5f * (hi - lo);
                if (sf.kind != SurfaceKind.Stabilator) rollHalfWing += 0.5f * (hi - lo);
                if (sf.kind != SurfaceKind.Stabilator && sf.rate > 0f) rollSlew = Mathf.Min(rollSlew, sf.rate / Mathf.Max(1f, 0.5f * (sf.max - sf.min)));
            }
            aRoll = Mathf.Clamp(aRollF.Step(rollHalf * IaInv[0, 0] * Mathf.Rad2Deg, 0.2f, dt), 10f, 5000f);
            rollCtrlHalf = aRoll * Mathf.Deg2Rad / Mathf.Max(IaInv[0, 0], 1e-9f);
            aRollWing = Mathf.Clamp(aRollWF.Step(rollHalfWing * IaInv[0, 0] * Mathf.Rad2Deg, 0.2f, dt), 10f, 5000f);
            aYaw = Mathf.Clamp(aYawF.Step(0.5f * (capPos.z - capNeg.z) * IaInv[2, 2] * Mathf.Rad2Deg, 0.2f, dt), 5f, 3000f);
            // axis priority: at high AoA keep the yaw axis (sideslip) ahead of body roll
            float kHi = Mathf.Clamp01((Mathf.Abs(alpha) - 20f) / 20f);
            // pitch (load / AoA protection) has priority over roll; escalates near the load limits
            float gLimHi = !flightAssist ? Plugin.MpoGPos : Plugin.GPos, gLimLo = !flightAssist ? Plugin.MpoGNeg : Plugin.GNeg;
            float nearLim = Mathf.Clamp01(Mathf.Max((nz - 0.85f * gLimHi) / (0.15f * gLimHi), (0.85f * gLimLo - nz) / (0.15f * -gLimLo)));
            // stabilator pitch authority left (0 when their own local AoA is post-stall); without it the pitch
            // axis stops outranking roll/yaw and the stabs are freed for differential (roll/yaw) use
            stabPitchFrac = 1f;
            foreach (var s in surfaces) if (s.kind == SurfaceKind.Stabilator && s.active) stabPitchFrac = Mathf.Min(stabPitchFrac, s.pitchFrac);
            float kPS = 1f - stabPitchFrac;
            alloc.axisWeight = new Vector3(Mathf.Lerp(Mathf.Lerp(1f, 0.35f, kHi), 1.5f, kPS),
                                           Mathf.Lerp(Mathf.Lerp(8f, 15f, nearLim), 1.0f, kPS),
                                           Mathf.Lerp(Mathf.Lerp(Plugin.YawPriority, Mathf.Max(2f, Plugin.YawPriority), kHi), Mathf.Max(2.5f, Plugin.YawPriority), kPS));
            foreach (var s in surfaces) if (s.kind == SurfaceKind.Stabilator && s.partner != null) s.antiCost = Mathf.Lerp(2.0e-4f, Mathf.Lerp(Plugin.StabRollCost, 1.0e-2f, Mathf.InverseLerp(160f, 240f, ias)), stabPitchFrac);

            nAlphaTimer += dt;
            if (nAlphaTimer > 0.1f)
            {
                nAlphaTimer = 0f;
                ComputeEnvelope(wind, rho, root.right, eL);
            }
            tw = tvc.totalThrust / (mass * 9.81f);

            // ---------------- mode ----------------
            bool wow = false, mainsWow = false, noseWow = false;
            LandingGear noseGear = NoseGear();
            foreach (LandingGear g in gears)
            {
                if (g == null || !g.WeightOnWheel(0.05f)) continue;
                wow = true;
                if (g == noseGear) noseWow = true; else mainsWow = true;
            }
            groundFrames = wow ? Mathf.Min(groundFrames + 1, 1000) : 0;
            // Takeoff / landing rotation: main wheels on the ground, nose wheel off. The direct ground law gave no
            // attitude hold here (the nose went wherever the surfaces put it until the mains left the ground), so
            // this phase flies the gear-down pitch-rate law: neutral stick holds the nose attitude, stick commands a
            // gentle rotation rate. Latched with hysteresis so a bouncing nose wheel does not flip the law.
            if (noseGear != null && mainsWow && !noseWow && tas > 15f) { rotating = true; noseDownTime = 0f; }
            else if (rotating)
            {
                noseDownTime = noseWow ? noseDownTime + dt : 0f;
                if (!wow || noseDownTime > 0.3f || tas < 12f) rotating = false;
            }
            FcsMode newMode;
            mainsOnGround = mainsWow;
            if (rotating && mainsWow) newMode = FcsMode.GearDownRate;
            else if (wow && (tas < 110f || groundFrames > 10)) newMode = FcsMode.Ground;
            else if (!flightAssist) newMode = FcsMode.MPO;
            else if (ac.gearDeployed) newMode = FcsMode.GearDownRate;
            else newMode = FcsMode.AoAG;
            if (newMode != Mode)
            {
                if (Mode == FcsMode.Ground || Mode == FcsMode.Off)
                {
                    qT = omega.y * Mathf.Rad2Deg; qTprev = qT; qTraj.Reset(qT);
                    pTraj.Reset(0f); pS = 0f; aInt = 0f; gInt = 0f;
                }
                Mode = newMode;
            }
            if (Mode != lastLoggedMode) { Plugin.Log.LogInfo("F-22E FCS mode: " + Mode); lastLoggedMode = Mode; }

            // ---------------- pilot ----------------
            // stick shaping: first-order smoothing (like SimplePlanes' input smoothing) then a rate limit
            float ks = dt / (Plugin.StickSmoothing + dt);
            spF += (Mathf.Clamp(-inp.pitch, -1f, 1f) - spF) * ks;
            srF += (Mathf.Clamp(inp.roll, -1f, 1f) - srF) * ks;
            syF += (Mathf.Clamp(inp.yaw, -1f, 1f) - syF) * ks;
            sp = Mathf.MoveTowards(sp, spF, 8f * dt);
            sr = Mathf.MoveTowards(sr, srF, RollStickRate * dt);
            sy = Mathf.MoveTowards(sy, syF, 8f * dt);

            Vector3 target;
            if (Mode == FcsMode.Ground)
            {
                // Direct law on the ground, like stock: every surface follows the stick through its original
                // pitch/roll/yaw mixing and the nozzles follow pitch stick, independent of airspeed (works at a
                // standstill). No feedback, no allocation.
                float ip = -sp, ir = sr, iy = sy;       // ControlInputs sign convention (pitch +1 = nose down)
                foreach (var s in surfaces)
                    s.cmd = Mathf.Clamp(ip * s.origPitchRange + ir * s.origRollRange + iy * s.origYawRange, s.min, s.max);
                tvc.cmd = -ip * tvc.max;
                qCmd = qT = omega.y * Mathf.Rad2Deg; qTprev = qT; qTraj.Reset(qT); pTraj.Reset(0f);
                pCmd = omega.x * Mathf.Rad2Deg; rCmd = omega.z * Mathf.Rad2Deg;
                omegaDotDes = Vector3.zero;
                unload = prot = false;
                pS = 0f; gInt = 0f; aInt = 0f;
                target = mF;
                tvcPitchInput = Mathf.Clamp(ip, -1f, 1f);
            }
            else
            {
                PitchLaw(dt, Vc);
                LateralLaw(dt);
                qTprev = qT;
                Vector3 cmdRad = new Vector3(pCmd, qT, rCmd) * Mathf.Deg2Rad;
                Vector3 err = cmdRad - omega;
                // trajectory accelerations fed forward: the inner loop is asked for what the shaped command
                // is doing, so it leads instead of chasing (crisp roll stops, no saturation on entry)
                omegaDotDes = new Vector3(
                    Plugin.KRoll * err.x + pDotFF * Mathf.Deg2Rad,
                    Plugin.KPitch * err.y + 0.5f * qTraj.v * Mathf.Deg2Rad,
                    Plugin.KYaw * err.z + rDotFF * Mathf.Deg2Rad);
                // INDI: new effector moment = filtered present effector moment + I * (desired - measured accel)
                target = mF + MulI(omegaDotDes - wdF);
                // gyroscopic / inertial coupling lead: the INDI increment only sees coupling moments after they have
                // accelerated the airframe; add the change in w x Iw between the commanded and present rates so fast
                // rolls at high speed do not first kick the pitch axis (NDI-style feed-forward)
                Vector3 wc = new Vector3(pCmd, qT, rCmd) * Mathf.Deg2Rad;
                target += Plugin.CouplingFF * (Vector3.Cross(wc, MulI(wc)) - Vector3.Cross(omega, MulI(omega)));
                // TVC role: its move cost sets whether pitch transients go to the stabs (high cost: nozzles only follow
                // the sustained share) or to the nozzles first (low cost). Post-stall (no stab pitch authority) the
                // nozzles are always fast.
                tvc.lam = Mathf.Lerp(2.0e-5f, Mathf.Max(2.0e-5f, Plugin.TvcMoveCost), stabPitchFrac);
                // Rolling at low AoA with no pitch command: the roll deflections add a pitch moment. The nozzles are
                // the fastest pitch trimmer and still catch it first, but only the fast part: the slow (sustained)
                // part of their deviation from the pre-roll trim is washed out (time constant RollNozzleWashout) and
                // handed to the stabs, which are already deflecting for the roll. So the nozzles no longer sit at a
                // negative deflection through the roll, and pitch disturbances stay as small as before.
                float kRollT = Mathf.Clamp01(Mathf.Abs(pCmd) / 40f) * stabPitchFrac * (1f - Mathf.InverseLerp(12f, 20f, Mathf.Abs(alpha)))
                             * (1f - Mathf.Clamp01(Mathf.Abs(sp) / 0.15f)) * (1f - Mathf.Clamp01(Mathf.Abs(qT) / 8f));
                float wash = WashK >= 0f ? WashK : Plugin.RollNozzleHold;
                if (kRollT < 0.05f) tvcTrimRef += (tvc.cmdOut - tvcTrimRef) * (dt / (0.3f + dt));
                // above ~150 m/s IAS the stabs have authority to spare: there the nozzles simply hold still in the
                // roll (move cost), which costs no pitch disturbance at all
                tvc.lam = Mathf.Max(tvc.lam, 0.05f * Mathf.Clamp01(wash) * kRollT * Mathf.InverseLerp(125f, 150f, ias));
                if (wash > 0f && kRollT > 0.02f)
                {
                    for (int i = 0; i < effs.Count; i++) cmdSave[i] = effs[i].cmd;
                    alloc.Solve(effs, target);                                   // pass 1: free allocation
                    float tauW = WashTau >= 0f ? WashTau : Plugin.RollNozzleWashout;
                    tvcSlow += ((tvc.cmd - tvcTrimRef) - tvcSlow) * (dt / (Mathf.Max(0.02f, tauW) + dt));
                    // stab headroom: never hand the stabs more than they can take without running into their travel
                    // limits (at low IAS the roll alone can take one stab to full travel); the rest stays on the nozzles
                    float util = 0f;
                    foreach (var sf in surfaces) if (sf.kind == SurfaceKind.Stabilator && sf.active) util = Mathf.Max(util, Mathf.Abs(sf.cmd) / Mathf.Max(1f, sf.max));
                    float head = Mathf.Clamp01((WashUtilHi - util) / 0.2f);
                    float u = Mathf.Clamp(tvc.cmd - Mathf.Clamp01(wash) * kRollT * head * tvcSlow, tvc.min, tvc.max);
                    for (int i = 0; i < effs.Count; i++) effs[i].cmd = cmdSave[i];
                    float lo0 = tvc.lo, hi0 = tvc.hi;
                    tvc.lo = tvc.hi = u;
                    alloc.Solve(effs, target);                                   // pass 2: stabs take the washed-out part
                    tvc.lo = lo0; tvc.hi = hi0;
                }
                else
                {
                    tvcSlow += (0f - tvcSlow) * (dt / (0.2f + dt));
                    alloc.Solve(effs, target);
                }
                // yaw-axis saturation: how far the allocation falls short of the yaw demand (deg/s^2)
                Vector3 ach = Vector3.zero;
                dbgYawRud = dbgYawWing = dbgYawStab = dbgYawTvc = 0f;
                foreach (Effector e in effs) if (e.active)
                {
                    Vector3 m = e.MAt(e.cmd); ach += m;
                    if (!DebugYaw) continue;
                    float yz = m.z * IaInv[2, 2] * Mathf.Rad2Deg;
                    if (e.name.Contains("Rudder")) dbgYawRud += yz;
                    else if (e.name.Contains("Flap") || e.name.Contains("Aileron")) dbgYawWing += yz;
                    else if (e.name.Contains("Elevator")) dbgYawStab += yz;
                    else dbgYawTvc += yz;
                }
                dbgTargetZ = target.z * IaInv[2, 2] * Mathf.Rad2Deg; dbgMfZ = mF.z * IaInv[2, 2] * Mathf.Rad2Deg; dbgTgtY = target.y * IaInv[1, 1] * Mathf.Rad2Deg; dbgMfY = mF.y * IaInv[1, 1] * Mathf.Rad2Deg;
                float shortZ = (target.z - ach.z) * Mathf.Sign(target.z - mF.z);
                yawSat = yawSatF.Step(Mathf.Clamp01((shortZ * IaInv[2, 2] * Mathf.Rad2Deg - 10f) / 40f), 0.25f, dt);

                // TVC command -> ControlInputs.pitch-equivalent for the Turbofan (nozzle.x = -pitch*gain*range)
                float range = 10f, gain = 1f;
                if (tvc.engines.Count > 0 && tvc.engines[0] != null)
                {
                    range = Mathf.Max(0.1f, Mathf.Abs(Acc.TF_vec(tvc.engines[0]).x));
                    float gx = Acc.TF_vecGain(tvc.engines[0]).x;
                    gain = Mathf.Abs(gx) > 1e-3f ? gx : 1f;
                }
                tvcPitchInput = Mathf.Clamp(-tvc.cmdOut / (range * gain), -1f, 1f);
            }
            mTarget = target;
            // actuator damping: the servo command is acceleration-limited (reaches the servo's full speed in
            // ActuatorDamping seconds and brakes into the target on a matching curve), so surfaces and nozzles start
            // and stop smoothly instead of snapping between allocation solutions. Unlike a first-order lag it adds
            // no delay to a steady slew: a ramping command is followed at full servo speed.
            float tauA = Plugin.ActuatorDamping;
            foreach (Effector e in effs)
            {
                if (Mode == FcsMode.Ground || tauA <= 1e-4f) { e.cmdOut = e.cmd; e.cmdLag = e.cmd; e.outTraj.Reset(e.cmd); continue; }
                if (LegacyLag) { e.cmdOut += (e.cmd - e.cmdOut) * (dt / (tauA + dt)); e.outTraj.Reset(e.cmdOut); continue; }
                float vmax = Mathf.Max(1f, e.rate);
                // a very short pre-filter takes the step-to-step allocation noise out; the acceleration limit does the
                // rest of the smoothing without delaying steady slews
                e.cmdLag += (e.cmd - e.cmdLag) * (dt / (PreLag + dt));
                e.outTraj.Step(e.cmdLag, vmax, vmax / tauA, dt);
                e.cmdOut = e.outTraj.x;
            }
            if (Mode == FcsMode.Ground) tvcPitchInput = Mathf.Clamp(-sp, -1f, 1f);
            foreach (var s in surfaces) if (s.active) s.localAoACmd = s.LocalAoAAt(s.cmd);

            Engaged = true;
            telemetry?.Write(time, sp, sr, sy, rho);
        }

        // ------------------------------------------------------------------------------------------
        // Longitudinal law
        // ------------------------------------------------------------------------------------------
        private float RateBudget()
        {
            float qTW = Mathf.Clamp(5f + 0.15f * ias + 40f * tw, 5f, Plugin.MaxPitchRate);
            float qIAS = Mathf.Clamp((ias - 22f) * 1.1f, 0f, Plugin.MaxPitchRate);
            float h = Mathf.Clamp01((Mathf.Abs(alpha) - (Plugin.CoupleAoA - 6f)) / 12f); // 30..42 deg
            return Mathf.Clamp(Mathf.Lerp(Mathf.Max(qTW, qIAS), qTW, h), 6f, Plugin.MaxPitchRate);
        }

        private const float KCapture = 3f;   // 1/s: linear capture gain of the kinematic limiters near the limit

        private void PitchLaw(float dt, float Vc)
        {
            qB = RateBudget();
            bool limitsOn = Mode != FcsMode.MPO;
            float aHi = limitsOn ? Plugin.AoAPos : 999f;
            float aLo = limitsOn ? Plugin.AoANeg : -999f;
            float gMax = Mode == FcsMode.MPO ? Plugin.MpoGPos : Plugin.GPos;
            float gMin = Mode == FcsMode.MPO ? Plugin.MpoGNeg : Plugin.GNeg;
            float gpd = 9.81f / Vc * Mathf.Rad2Deg;          // deg/s of flight-path rate per g
            const float tLag = 0.15f;

            // Effective flight-path rate from MEASURED pitch rate and AoA rate. Unlike a modelled value it
            // includes roll/sideslip kinematic coupling (p tan(beta) etc.), so the laws stay honest in rolls.
            float wVe = omega.y * Mathf.Rad2Deg - alphaDot;
            dbgWVe = wVe;

            // AoA rate needed just to HOLD the present load while the airspeed bleeds (n ~ V^2 at fixed AoA)
            alphaDotHoldG = Mathf.Clamp(2f * Mathf.Abs(nz) * Mathf.Max(0f, -vDot) / (Vc * Mathf.Max(nAlpha, 0.02f)), 0f, 15f);

            unload = false; prot = false;
            float ib = 1f;
            float sigma = 0f;
            alphaCmd = 0f; nCmd = 1f; betaG = 0f;

            if (Mode == FcsMode.AoAG)
            {
                alphaCmd = sp >= 0f ? sp * aHi : sp * (-aLo);
                nCmd = sp >= 0f ? 1f + sp * (gMax - 1f) : 1f + sp * (1f - gMin);
                ib = Mathf.Clamp01((Mathf.Abs(sp) - 0.01f) / 0.06f);

                // blend by what the airframe can pull inside the pitch-rate budget at this speed: the lesser of
                // the kinematic load inside the budget and the aerodynamic maximum from the envelope sweep
                float gAtBudget = 1f + qB * Mathf.Deg2Rad * Vc / 9.81f;
                gAvail = Mathf.Min(gAtBudget, nMaxAero);
                betaG = Mathf.Clamp01((gAvail - gMax) / 3f + 0.5f);

                // Unload = stick eased toward neutral on the same side. The AoA (load) is walked down along a
                // smooth profile: the commanded AoA rate falls off as sqrt of the remaining error, so the pitch
                // rate eases down, bottoms out no lower than zero (no nose shove; with no lift left it simply
                // holds zero rate) and eases back up to the flight-path rate as the new target is reached.
                bool same = sp * alpha >= 0f;
                bool unloadA = same && Mathf.Abs(alpha) > 3f && Mathf.Abs(alphaCmd) < Mathf.Abs(alpha) - 1f;
                bool unloadG = sp * (nz - 1f) >= 0f && Mathf.Abs(nCmd - 1f) < Mathf.Abs(nz - 1f) - 0.3f;
                unload = betaG < 0.5f ? unloadA : unloadG;
                if (nz > gMax || nz < gMin || alpha > aHi || alpha < aLo || ib < 0.5f) unload = false;
                sigma = betaG < 0.5f ? Mathf.Sign(alpha) : Mathf.Sign(nz - 1f);

                // ---- AoA sub-law: dynamic inversion of alpha_dot = q - flight-path rate ----
                float e = alphaCmd - alpha;
                float aDotDes = Mathf.Sign(e) * Mathf.Min(Plugin.KpAoA * Mathf.Abs(e), Mathf.Sqrt(2f * 120f * Mathf.Abs(e)));
                // integrator: removes the residual AoA error (servo/INDI lag, moving flight path) near the target
                if (betaG < 0.5f && ib > 0.5f && !unloadA && Mathf.Abs(e) < 4f && Mathf.Abs(qT) < 0.95f * qB)
                    aInt = Mathf.Clamp(aInt + Plugin.KiAoA * e * dt, -10f, 10f);
                else
                    aInt = Mathf.MoveTowards(aInt, 0f, 20f * dt);
                float qA = wVe + aDotDes + aInt;
                // Unloading: the target is the pitch rate the aircraft will SUSTAIN at the commanded AoA (its
                // flight-path rate there: present flight-path rate + the lift change from the envelope sweep),
                // reached by a smooth deceleration (trajectory below). A small AoA-error term guarantees
                // convergence; if AoA rises during the decel, the target is pulled down harder (to zero if needed).
                if (unloadA)
                {
                    float qPred = wVe + (NPath(alphaCmd) - NPath(alpha)) * gpd;
                    float rising = Mathf.Max(0f, sigma * alphaDot);
                    qA = qPred + (0.15f + 0.45f * (1f - Mathf.Clamp01(Mathf.Abs(e) / 10f))) * e - sigma * Plugin.UnloadRiseGain * rising;
                }

                // ---- G sub-law: flight-path rate for the commanded load + load error (lift-slope normalised) ----
                float nAl = Mathf.Max(nAlpha, 0.1f);
                float qG;
                if (unloadG)
                {
                    float rising = Mathf.Max(0f, sigma * alphaDot);
                    qG = (nCmd - eLy) * gpd + 0.3f * Plugin.KpG * (nCmd - nz) / nAl - sigma * Plugin.UnloadRiseGain * rising;
                }
                else
                    qG = (nCmd - eLy) * gpd + Plugin.KpG * (nCmd - nz) / nAl + gInt + Mathf.Sign(nCmd - 1f) * alphaDotHoldG;

                float qAG = Mathf.Lerp(qA, qG, betaG);
                // slow integral on load error while the G law is in charge and not saturated
                if (betaG > 0.5f && ib > 0.5f && !unload && Mathf.Abs(qT) < 0.95f * qB && Mathf.Abs(nCmd - nz) < 1.5f)
                    gInt = Mathf.Clamp(gInt + Plugin.KiG * (nCmd - nz) / Mathf.Max(nAlpha, 0.1f) * dt, -10f, 10f);
                else
                    gInt = Mathf.MoveTowards(gInt, 0f, 10f * dt);
                qRaw = qAG * ib;                               // neutral stick => zero pitch-rate hold
            }
            else
            {
                qRaw = sp * qB;                                // rate command (gear down / MPO)
                if (rotating && mainsOnGround) qRaw = sp * Mathf.Min(qB, Plugin.RotationRate);   // on the mains: gentle rotation
                gInt = 0f; aInt = 0f;
            }

            float qMax = qB, qMin = -qB;
            if (limitsOn)
            {
                float top = aHi, bot = aLo;
                if (Mode == FcsMode.AoAG && !unload && ib > 0.5f && betaG < 0.5f)
                {
                    if (alphaCmd > 5f) top = Mathf.Min(aHi, alphaCmd + 0.2f);
                    if (alphaCmd < -5f) bot = Mathf.Max(aLo, alphaCmd - 0.2f);
                }
                // Kinematic AoA limiter: the pitch rate is capped so the available braking acceleration
                // (effector model, incl. TVC) can stop alpha exactly at the limit/target.
                float dPos = Mathf.Max(0f, (top - alpha) * 0.9f - Mathf.Max(0f, alphaDot) * tLag);
                float dNeg = Mathf.Max(0f, (alpha - bot) * 0.9f - Mathf.Max(0f, -alphaDot) * tLag);
                qMax = Mathf.Min(qMax, wVe + Mathf.Min(Mathf.Sqrt(2f * aDn * dPos), KCapture * dPos));
                qMin = Mathf.Max(qMin, wVe - Mathf.Min(Mathf.Sqrt(2f * aUp * dNeg), KCapture * dNeg));
            }

            if (tas > 25f)
            {
                // Kinematic + linear G limiter (signed AoA-distance to the load limit through the live lift slope)
                float nA = Mathf.Max(nAlpha, 0.02f);
                float ffp = nz > 1f ? alphaDotHoldG : 0f, ffn = nz < 0f ? alphaDotHoldG : 0f;
                // rolling: the pitch loop shares authority with the roll/yaw axes and the coupling moments, so the
                // load limiter looks further ahead
                float tLagG = tLag + RollLoadLead * Mathf.Clamp01(Mathf.Abs(omega.x * Mathf.Rad2Deg) / 150f);
                float dGp = (gMax - nz) / nA - Mathf.Max(0f, alphaDot - ffp) * tLagG;
                float dGn = (nz - gMin) / nA - Mathf.Max(0f, -alphaDot - ffn) * tLagG;
                float capP = dGp > 0f ? Mathf.Min(Mathf.Sqrt(2f * aDn * dGp), KCapture * dGp) : KCapture * dGp;
                float capN = dGn > 0f ? Mathf.Min(Mathf.Sqrt(2f * aUp * dGn), KCapture * dGn) : KCapture * dGn;
                qMax = Mathf.Min(qMax, wVe + ffp + capP);
                qMin = Mathf.Max(qMin, wVe - ffn - capN);
            }

            float q;
            // AoA envelope protection. Neutral stick: the nose is driven back so AoA falls at up to the
            // recovery rate (48 deg/s, not thrust/weight limited), never faster than 48 deg/s of body pitch rate.
            // Opposing stick scales that recovery pitch rate by (1 - stick): half stick = half rate, full stick =
            // zero pitch rate (protection cancelled, attitude held; MPO is the way to go further).
            // Override latch: stick held fully against a protection that was entered from well beyond the limit
            // (MPO / departure) holds zero pitch rate until the stick is eased - also once AoA has come back inside
            // the limit or passed through 180. Without it the jet dropped out of the hold as AoA fell back through
            // the limit and the AoA law then chased the swinging flight path at low airspeed (nose shove).
            // Stick travel: 90 % counts as full (many sticks / curves never report exactly 1.0); arm at 85 %, release
            // below 70 %. The latch also releases once AoA is back 5 deg inside the limit, so a stick still held aft
            // flies the normal law again (full aft = the AoA limit) instead of staying frozen at zero pitch rate.
            if (protHold && (!limitsOn || Mode != FcsMode.AoAG || Mathf.Abs(sp) < 0.7f
                             || (protHoldSide > 0f && alphaU < aHi - 5f) || (protHoldSide < 0f && alphaU > aLo + 5f))) protHold = false;
            if (limitsOn && Mode == FcsMode.AoAG && !protHold)
            {
                if (alphaU > aHi + 5f && sp > 0.85f) { protHold = true; protHoldSide = 1f; }
                else if (alphaU < aLo - 5f && sp < -0.85f) { protHold = true; protHoldSide = -1f; }
            }
            if (protHold)
            {
                q = 0f;
                qMax = qMin = q;
                prot = true;
            }
            else if (limitsOn && alphaU > aHi + 0.5f)
            {
                float rec = Mathf.Min(Plugin.ProtRate, Mathf.Sqrt(2f * Mathf.Max(aUp, 40f) * (alphaU - aHi)) + 4f);
                float qRec = Mathf.Max(wVe - rec, -Plugin.ProtRate);
                float k = 1f - Mathf.Clamp01(sp / 0.9f);
                q = qRec < 0f ? qRec * k : qRec;
                qMax = qMin = q;
                prot = true;
            }
            else if (limitsOn && alphaU < aLo - 0.5f)
            {
                float rec = Mathf.Min(Plugin.ProtRate, Mathf.Sqrt(2f * Mathf.Max(aDn, 40f) * (aLo - alphaU)) + 4f);
                float qRec = Mathf.Min(wVe + rec, Plugin.ProtRate);
                float k = 1f - Mathf.Clamp01(-sp / 0.9f);
                q = qRec > 0f ? qRec * k : qRec;
                qMax = qMin = q;
                prot = true;
            }
            else
            {
                q = qMin > qMax ? (alpha >= 0f ? qMax : qMin) : Mathf.Clamp(qRaw, qMin, qMax);
                if (unload)
                {
                    // never reverse while unloading: zero pitch rate is the floor (no nose shove)
                    if (sigma > 0f) q = Mathf.Max(q, 0f);
                    else if (sigma < 0f) q = Mathf.Min(q, 0f);
                }
                // Stick held against the nose-down (nose-up) direction: no pitch rate opposite to the stick, except
                // to stop a load-limit exceedance, and then only (1 - stick) of the 48 deg/s budget. Stops
                // AoA-overshoot corrections / limiters from shoving the nose while the pilot is pulling.
                if (Mode == FcsMode.AoAG)
                {
                    if (sp > 0.02f && alpha > -2f) q = Mathf.Max(q, nz > gMax ? -Plugin.ProtRate * (1f - sp) : 0f);
                    else if (sp < -0.02f && alpha < 2f) q = Mathf.Min(q, nz < gMin ? Plugin.ProtRate * (1f + sp) : 0f);
                }
            }
            qMaxDbg = qMax; qMinDbg = qMin;
            qCmd = Mathf.Clamp(q, -Plugin.ProtRate, Plugin.ProtRate);

            // Feasible pitch-rate trajectory: accelerate no faster than the effectors can (measured authority,
            // TVC included), change acceleration no faster than the servos can follow; the trajectory's
            // acceleration is fed forward to the inner loop. Unloading uses a gentle profile.
            float dq = qCmd - qTraj.x;
            float A = Mathf.Max(0.7f * (dq > 0f ? aUp : aDn), 30f);
            // at high dynamic pressure a degree of AoA is several g: pitch-rate changes are paced by load onset
            // (about PitchOnsetGps g/s per unit of commanded pitch acceleration) so the inner loop can follow
            A = Mathf.Min(A, Mathf.Max(30f, Plugin.PitchOnsetGps / Mathf.Max(0.05f, nAlpha)));
            float J = prot ? A / 0.1f : A / 0.15f;
            if (unload)
            {
                // smooth deceleration toward the sustained rate; harder if AoA is rising during the decel
                bool rising = sigma * alphaDot > 0.5f;
                A = rising ? Mathf.Min(A, Mathf.Max(60f, 2f * Plugin.UnloadDecel)) : Mathf.Min(A, Plugin.UnloadDecel);
                J = A / 0.3f;
            }
            qTraj.Step(qCmd, A, J, dt);
            qT = qTraj.x;
        }

        // ------------------------------------------------------------------------------------------
        // Lateral-directional law: stability-axis (velocity-vector) roll with sideslip regulation.
        // At high AoA a stability-axis roll is mostly body yaw (r = p_s sin(alpha)); above CoupleAoA the
        // pedals feed the same velocity-vector roll command as the stick (one control axis).
        // Body roll and yaw rate limits are enforced jointly (the command vector is scaled, never
        // clipped per-axis), so neither limit can be exceeded and coordination is preserved.
        // ------------------------------------------------------------------------------------------
        // Low-IAS yaw-rate margin curve: the full LowIasYawBoost at/below LowIasYawBoostFullIAS (falling leaf,
        // where a slower yaw stop is acceptable), decaying exponentially above it so the 55-85 m/s IAS high-AoA
        // rolls keep a crisp stop (a wide margin there only raised the peak the yaw axis then had to stop).
        internal static float YawBoostFixed = -1f;   // bench override
        internal static bool RudderRevBack = true;
        internal static float YawFFAoA0 = 15f, YawFFAoA1 = 28f;
        internal static bool ReversalFloor = true;
        internal static float RevFloorK = 0.6f;
        internal static bool RevConst = true, RevCarry = true;
        private float revA, revTarget;
        internal static float WashK = -1f, WashTau = -1f, WashUtilHi = 0.75f;   // bench overrides (<0 = config)
        private float tvcTrimRef, tvcSlow;
        private float[] cmdSave = new float[64];
        internal static float LowIasYawBoostAt(float iasNow)
        {
            if (YawBoostFixed >= 1f) return YawBoostFixed;
            float b = Mathf.Max(1f, Plugin.LowIasYawBoost) - 1f, full = Plugin.LowIasYawBoostFullIas;
            float k = iasNow <= full ? 1f : (float)System.Math.Exp(-(iasNow - full) / Mathf.Max(1f, Plugin.LowIasYawBoostFalloff));
            k *= Mathf.Clamp01((120f - iasNow) / 10f);
            return 1f + b * k;
        }

        private void LateralLaw(float dt)
        {
            float a = alpha * Mathf.Deg2Rad;
            float ca = Mathf.Cos(a), sa = Mathf.Sin(a);
            // Roll-rate ceiling: the original aircraft's roll rate (+20 % low IAS / +10 % high IAS), but never more
            // than this airframe can hold with a fraction of its roll authority (steady rate where roll damping
            // equals RollAuthorityUse x the available roll control moment) - "just enough" deflection, rate is
            // given up instead of saturating the surfaces.
            pFull = rollCtrlHalf / Mathf.Max(1f, -Lp) * Mathf.Rad2Deg;
            pStockRef = StockRollRef();
            float pMax = Mathf.Min(Plugin.MaxRollRate, Mathf.Min(pStockRef, Plugin.RollAuthorityUse * pFull));
            // Rolling-g limit: full roll rate to 6 g, easing to 50 % at the positive load limit (and to 60 %
            // at the negative limit) so roll cannot starve the pitch axis of stabilator/TVC authority.
            float gHi = Mode == FcsMode.MPO ? Plugin.MpoGPos : Plugin.GPos, gLo = Mode == FcsMode.MPO ? Plugin.MpoGNeg : Plugin.GNeg;
            float rg = 1f;
            // (the original-aircraft roll rates at high speed are large enough that inertial roll-pitch coupling
            // eats the load margin, so the reduction starts at 4 g and also scales with dynamic pressure)
            float qbarK = Mathf.Clamp01((ias - 250f) / 150f);
            float gStart = Mathf.Lerp(6f, 3f, qbarK);
            if (nz > gStart) rg = Mathf.Lerp(1f, Mathf.Lerp(0.5f, 0.35f, qbarK), (nz - gStart) / Mathf.Max(0.5f, gHi - gStart));
            else if (nz < -1.5f) rg = Mathf.Lerp(1f, 0.6f, (-1.5f - nz) / Mathf.Max(0.5f, -1.5f - gLo));
            pMax *= Mathf.Clamp(rg, 0.4f, 1f);
            pMax = Mathf.Max(pMax, 30f);
            pMaxDbg = pMax;
            // Body yaw-rate ceiling: the configured 65 deg/s, but never more than the yaw axis can stop again in
            // ~0.45 s with the measured yaw authority (rudder/servo-limited at low q). Keeps velocity-vector rolls
            // at moderate/high AoA stoppable without large sideslip excursions.
            // (only where the yaw axis dominates: at low AoA the stability-axis roll needs little body yaw and the
            // authority cap would otherwise throttle ordinary rolls at low speed)
            // (at low IAS the margin is widened by LowIasYawBoost: more yaw rate for falling-leaf / low-speed pedal
            // work, at the price of a slower yaw stop in the thin airflow)
            float yawBoost = LowIasYawBoostAt(ias);
            float rMax = Mathf.Lerp(Plugin.MaxYawRate, Mathf.Clamp(yawBoost * Plugin.YawStopFactor * aYaw, 20f * yawBoost, Plugin.MaxYawRate), Mathf.Clamp01((Mathf.Abs(alpha) - 15f) / 10f));
            rMaxDbg = rMax;
            float kc = Mathf.Clamp01((Mathf.Abs(alpha) - Plugin.CoupleAoA) / 14f);

            float pSmax = Mathf.Min(pMax / Mathf.Max(Mathf.Abs(ca), 0.05f), rMax / Mathf.Max(Mathf.Abs(sa), 0.05f));
            float pSTarget = Mathf.Clamp(sr + kc * sy, -1f, 1f) * pSmax;

            float betaCmd = -(1f - kc) * sy * Plugin.PedalBeta;

            // Yaw-axis limits on the velocity-vector roll (the body yaw it needs grows with sin(alpha)):
            //  - sideslip guard: sideslip error beyond 4 deg backs the roll command off (smoothed, no limit cycle)
            //  - yaw saturation: if the allocator cannot deliver the yaw demand, the roll command is reduced
            //    before sideslip builds
            float betaErr = Mathf.Abs(beta - betaCmd);
            float kYawRel = Mathf.Clamp01((Mathf.Abs(alpha) - 8f) / 10f);
            float guard = Mathf.Clamp01(1f - (betaErr - 4f) / 10f) * (1f - 0.7f * yawSat * kYawRel);
            guard = guardF.Step(guard, 0.25f, dt);
            pSTarget *= guard;

            // Shaped velocity-vector roll command: accelerate no faster than BOTH body axes it maps into can
            // follow (p = pS cos a, r = pS sin a) with ~40 % of the measured authority kept in reserve for
            // regulation, and ramp/unwind the acceleration at a rate the servos can follow. Its acceleration is
            // fed forward, so roll entries and stops are crisp without the surfaces slamming to full travel.
            // accelerating: only the authority left over after roll damping at the present rate (never plans on
            // full deflection); braking: damping helps
            float pBody = Mathf.Abs(pTraj.x * ca);
            float accFrac = Mathf.Clamp(0.85f - pBody / Mathf.Max(30f, pFull), 0.1f, 0.5f);
            float aR = accFrac * aRoll / Mathf.Max(Mathf.Abs(ca), 0.05f), aY = 0.6f * aYaw / Mathf.Max(Mathf.Abs(sa), 0.05f);
            float A = Mathf.Min(aR, aY);
            // stopping / reversing: body roll is allowed to lead the yaw axis (brief sideslip transient that the
            // sideslip loop cleans up) so stops stay crisp at moderate AoA instead of waiting for the yaw axis
            bool braking = pTraj.x * (pSTarget - pTraj.x) < 0f;
            if (braking)
            {
                // braking budget = wing surfaces (fraction RollBrakeUse) + roll damping at the present commanded rate.
                // Damping does most of the work at high rates and vanishes at the end, so the planned deceleration
                // tapers as the roll rate does and the surfaces are back near neutral when the roll stops (no
                // full-deflection finish that has to be unwound after the stop = no rebound).
                float kDamp = aRoll / Mathf.Max(30f, pFull);              // 1/s: roll damping accel per unit rate
                A = Mathf.Min((Plugin.RollBrakeUse * aRollWing + kDamp * pBody) / Mathf.Max(Mathf.Abs(ca), 0.05f), 1.3f * aY);
                // reversal (the new target is on the other side of zero): the taper above is for stops, so the
                // surfaces are near neutral when the roll stops. In a reversal it made them swing back toward neutral
                // as the rate passed through zero and out again for the new roll (a hitch). A reversal is planned at
                // one constant acceleration instead (no damping-assisted peak that the jet then lags as it tapers),
                // and the new roll starts with that same acceleration (carried over below), so there is no step at
                // zero either.
                if (pSTarget * pTraj.x < 0f && Mathf.Abs(pSTarget) > 10f && ReversalFloor)
                {
                    float aNew = Mathf.Min(RevFloorK * aRoll / Mathf.Max(Mathf.Abs(ca), 0.05f), aY);
                    A = RevConst ? Mathf.Min(aNew, 1.3f * aY) : Mathf.Max(A, aNew);
                    revA = A; revTarget = pSTarget;
                }
            }
            else if (revA > 0f && RevCarry)
            {
                // just after a reversal's zero crossing: the new roll starts with the acceleration the reversal ended
                // with (no step), blending to the normal entry limit by half the new target rate
                if (pSTarget * revTarget <= 0f || Mathf.Abs(pSTarget) < 10f) revA = 0f;
                else
                {
                    float w = 1f - Mathf.Clamp01(Mathf.Abs(pTraj.x) / Mathf.Max(10f, 0.5f * Mathf.Abs(pSTarget)));
                    A = Mathf.Max(A, Mathf.Lerp(A, revA, w));
                    if (w <= 0f) revA = 0f;
                }
            }
            A = Mathf.Clamp(A, 20f, Plugin.MaxRollAccel);
            // jerk: the roll acceleration can only change as fast as the roll surfaces can slew (half travel per
            // 1/rollSlew s moves the roll accel by aRoll); planning faster leaves them behind, and the lag shows up
            // as a drift followed by a rebound when the command ends
            float Jr = Mathf.Min(A / 0.18f, RollJerkServo * aRoll * rollSlew / Mathf.Max(Mathf.Abs(ca), 0.05f));
            pTraj.Step(pSTarget, A, Mathf.Max(Jr, 100f), dt);
            pS = pTraj.x;

            float coord = 0f;
            if (tas > 40f)
                coord = 9.81f * Mathf.Sin(phi * Mathf.Deg2Rad) * Mathf.Cos(theta * Mathf.Deg2Rad) / tas * Mathf.Rad2Deg
                        * Mathf.Clamp01((tas - 40f) / 20f);
            float kb = Mode == FcsMode.GearDownRate ? 1.0f : Plugin.KBeta;
            // sideslip regulation, smoothed so the rudders do not chase servo-rate-limited transients (wobble)
            float rS = rSF.Step(Mathf.Clamp(kb * (beta - betaCmd), -rMax, rMax) + coord, 0.12f, dt);

            float p = pS * ca - rS * sa;
            float r = pS * sa + rS * ca;
            float sc = Mathf.Max(1f, Mathf.Max(Mathf.Abs(p) / pMax, Mathf.Abs(r) / rMax));
            pCmd = p / sc; rCmd = r / sc;
            // feed forward the full rate of change of the body-rate commands (roll trajectory AND the yaw rate
            // needed as AoA / sideslip regulation change during the roll), so the yaw axis leads instead of lagging
            pDotFF = pTraj.v * ca / sc;
            // lead the shaped roll command by the loop's tracking lag (servo rate + actuator damping + INDI filters):
            // the airframe follows the trajectory instead of trailing it, so the stop ends when the command does
            // (no residual rate for the feedback to clean up afterwards = no drift tail / rebound)
            // The lag comes from the INDI loop not seeing the roll damping moment fall as the rate falls until its
            // filters catch up, so it grows with the damping (dynamic pressure). At low IAS there is little of it and
            // the stop already needs most of the surface travel, so a lead would only saturate them (rebound).
            float kDampL = aRoll / Mathf.Max(30f, pFull);
            pCmd += Plugin.RollLead * Mathf.Max(0f, kDampL - 2.5f) * pDotFF;
            // (the reduced low-IAS feed-forward is for the rudders at low AoA; at moderate/high AoA the body yaw IS the
            // velocity-vector roll, so it gets the full lead there or the roll trails its command by the yaw loop's lag)
            float ffK = Mathf.Max(Mathf.InverseLerp(200f, 300f, ias), Mathf.InverseLerp(YawFFAoA0, YawFFAoA1, Mathf.Abs(alpha)));
            rDotFF = Mathf.Lerp(Plugin.YawRateFF, 1f, ffK) * rDotF.Step((rCmd - rCmdPrev) / dt, 0.1f, dt);
            pCmdPrev = pCmd; rCmdPrev = rCmd;
            // hard yaw-rate ceiling: if the airframe is already beyond it, stop feeding more yaw
            if (Mathf.Abs(omega.z * Mathf.Rad2Deg) > rMax && rDotFF * omega.z > 0f) rDotFF = 0f;
        }

        public void WriteHeader(System.IO.TextWriter w)
        {
            w.Write("t,mode,ias,tas,rho,alpha,beta,nz,nAlpha,tw,p,q,r,sp,sr,sy,qB,qRaw,qCmd,qT,pCmd,rCmd,aUp,aDn,betaG,alphaCmd,nCmd,unload,prot,omegaV,phi,theta,tvcCmd,tvcNow,nMaxAero,alphaAtNMax,gAvail,gInt,aInt,flapBias,flapTarget,hlPos,relief,rudderAuth,yawSat,aRoll,aYaw,pMax,pStockRef,pFull,stabPitchFrac");
            foreach (var s in surfaces) w.Write($",{s.name}_cmd,{s.name}_now,{s.name}_aoaNow,{s.name}_aoaCmd");
            w.WriteLine();
        }

        public void WriteRow(System.IO.TextWriter w, float t, float spv, float srv, float syv, float rho)
        {
            var ci = System.Globalization.CultureInfo.InvariantCulture;
            w.Write(string.Format(ci,
                "{0:F3},{1},{2:F1},{3:F1},{4:F3},{5:F2},{6:F2},{7:F2},{8:F3},{9:F2},{10:F1},{11:F1},{12:F1},{13:F2},{14:F2},{15:F2},{16:F1},{17:F1},{18:F1},{19:F1},{20:F1},{21:F1},{22:F0},{23:F0},{24:F2},{25:F1},{26:F2},{27},{28},{29:F1},{30:F1},{31:F1},{32:F2},{33:F2}",
                t, (int)Mode, ias, tas, rho, alpha, beta, nz, nAlpha, tw,
                omega.x * Mathf.Rad2Deg, omega.y * Mathf.Rad2Deg, omega.z * Mathf.Rad2Deg, spv, srv, syv,
                qB, qRaw, qCmd, qT, pCmd, rCmd, aUp, aDn, betaG, alphaCmd, nCmd, unload ? 1 : 0, prot ? 1 : 0,
                omegaV, phi, theta, tvc.cmd, tvc.now));
            w.Write(string.Format(ci, ",{0:F2},{1:F0},{2:F2},{3:F2},{4:F2},{5:F1},{6:F1},{7:F2},{8:F1},{9:F2},{10:F2},{11:F0},{12:F0}", nMaxAero, alphaAtNMax, gAvail, gInt,
                aInt, flaps.flapBias, flaps.flapTarget, flaps.hlPos, flaps.relief, rudderAuth, yawSat, aRoll, aYaw));
            w.Write(string.Format(ci, ",{0:F0},{1:F0},{2:F0},{3:F2}", pMaxDbg, pStockRef, pFull, stabPitchFrac));
            foreach (var s in surfaces)
                w.Write(string.Format(ci, ",{0:F2},{1:F2},{2:F1},{3:F1}", s.cmd, s.now, s.localAoANow, s.localAoACmd));
            w.WriteLine();
        }
    }
}
