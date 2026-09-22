// F-22E FCS — flap / load-relief scheduling.
//
// The 1.0.4 airframe carries two open-loop devices that move the flaperons' aerodynamic surfaces under the
// control-surface servo (hierarchy Flap_* > FlapAOADrive > HighLiftServo > FlapServo_* = lift normal):
//   HighLiftDevice (WingRear_L/R): speed schedule, fully out below 100 m/s, in above 130 m/s, 1/s slew;
//                                  swings HighLiftServo 15 deg TE-down, slats, +4 m^2 on WingRear.
//   AryxAlphaResponder (Flap_*):   AoA schedule on FlapAOADrive, up to 8 deg TE-down (peak ~18 deg AoA).
//   AryxAlphaResponder (Aileron_*): AoA schedule on AileronAOADrive, TE-up up to 9.6 deg, held post-stall.
// While the FCS flies the aircraft both are taken over here (their FixedUpdate is skipped):
//   - same baseline: speed schedule for the high-lift part, maneuver flap over normal AoA;
//   - only when the wing is asked for positive lift (AoA and load factor gates: inverted/pushing,
//     unloaded vertical flight -> retracted);
//   - capped so the flaperon's OWN local AoA (effector model) stays below its C_L peak including the
//     control deflection the roll axis may need on the TE-down side (roll-biased flaperons);
//   - high-lift part and maneuver flap fade out before the wing stalls;
//   - outer flaperon TE-up load relief keyed to load factor and off near/after the stall.
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Aryx_F22E_StrikeRaptor.FCS
{
    internal sealed class FlapSystem
    {
        public static readonly Type ResponderType = AccessTools.TypeByName("Aryx_F22E_StrikeRaptor.AryxAlphaResponder");
        static readonly FieldInfo fRotator = ResponderType != null ? AccessTools.Field(ResponderType, "rotator") : null;
        static readonly FieldInfo fFunctional = ResponderType != null ? AccessTools.Field(ResponderType, "isFunctional") : null;
        static readonly FieldInfo fDeflection = ResponderType != null ? AccessTools.Field(ResponderType, "deflection") : null;
        static readonly FieldInfo fMovingParts = AccessTools.Field(typeof(HighLiftDevice), "movingParts");

        sealed class MovingPart
        {
            public Transform t; public bool move, rotate;
            public Vector3 pR, pD, aR, aD;
            public void Animate(float x)
            {
                if (t == null) return;
                if (move) t.localPosition = Vector3.Lerp(pR, pD, x);
                if (rotate) t.localEulerAngles = Vector3.Lerp(aR, aD, x);
            }
        }

        sealed class Hld
        {
            public HighLiftDevice d; public AeroPart part; public readonly List<MovingPart> parts = new List<MovingPart>();
            public float pos;
        }

        sealed class Drive
        {
            public Component resp; public Transform rotator; public Quaternion baseRot;
            public SurfaceEffector surf; public bool flap;
            public Hld hl; public float hlAngle;      // HL swing seen by this flaperon (deg about x, deployed)
            public float angle;                       // present rotator angle about x (deg, Unity sign: + = TE up)
            public bool Functional => resp != null && (fFunctional == null || (bool)fFunctional.GetValue(resp));
        }

        readonly List<Hld> hlds = new List<Hld>();
        readonly List<Drive> drives = new List<Drive>();
        public readonly HashSet<int> ownedIds = new HashSet<int>();
        public bool Active;                          // true while the FCS drives the devices
        public float flapTarget, flapBias, hlPos, relief, ailBias;
        public bool Found => drives.Count > 0 || hlds.Count > 0;

        public FlapSystem(Aircraft ac, List<SurfaceEffector> surfaces)
        {
            foreach (HighLiftDevice d in Acc.Collect<HighLiftDevice>(ac))
            {
                var h = new Hld { d = d, part = Acc.HL_part(d), pos = Acc.HL_position(d) };
                if (fMovingParts != null && fMovingParts.GetValue(d) is Array arr)
                    foreach (object mp in arr)
                    {
                        if (mp == null) continue;
                        Traverse tr = Traverse.Create(mp);
                        h.parts.Add(new MovingPart
                        {
                            t = tr.Field("transform").GetValue<Transform>(), move = tr.Field("move").GetValue<bool>(), rotate = tr.Field("rotate").GetValue<bool>(),
                            pR = tr.Field("positionRetracted").GetValue<Vector3>(), pD = tr.Field("positionDeployed").GetValue<Vector3>(),
                            aR = tr.Field("anglesRetracted").GetValue<Vector3>(), aD = tr.Field("anglesDeployed").GetValue<Vector3>(),
                        });
                    }
                hlds.Add(h);
                ownedIds.Add(d.GetInstanceID());
            }
            if (ResponderType != null && fRotator != null)
            {
                foreach (Component r in Acc.Collect(ac, ResponderType))
                {
                    Transform rot = fRotator.GetValue(r) as Transform;
                    if (rot == null) continue;
                    SurfaceEffector se = surfaces.Find(s => s.cs != null && s.cs.gameObject == r.gameObject);
                    if (se == null || (se.kind != SurfaceKind.Flaperon && se.kind != SurfaceKind.Aileron)) continue;
                    var dv = new Drive { resp = r, rotator = rot, baseRot = Quaternion.identity, surf = se, flap = se.kind == SurfaceKind.Flaperon };
                    float x = rot.localEulerAngles.x; if (x > 180f) x -= 360f;
                    dv.angle = x;
                    foreach (Hld h in hlds)
                        foreach (MovingPart mp in h.parts)
                            if (mp.t != null && mp.rotate && mp.t.IsChildOf(rot) && mp.t != rot) { dv.hl = h; dv.hlAngle = mp.aD.x - mp.aR.x; }
                    drives.Add(dv);
                    ownedIds.Add(r.GetInstanceID());
                }
            }
        }

        public string Describe() =>
            $"{hlds.Count} high-lift devices, {drives.FindAll(d => d.flap).Count} flaperon drives, {drives.FindAll(d => !d.flap).Count} outer-flaperon drives";

        private static float Wrap(float x) { x %= 360f; if (x > 180f) x -= 360f; else if (x < -180f) x += 360f; return x; }

        /// <summary>Called once per FCS step before the effectors are evaluated.</summary>
        public void Update(float dt, FcsController c, bool ground, float speed, float stickRoll)
        {
            Active = true;
            float a = c.alpha, nz = c.nz;

            // ---------------- flaperon bias (TE-down, deg) ----------------
            float sched = 1f;
            float hlSched = 1f;
            if (hlds.Count > 0)
            {
                float sd = Acc.HL_speedDep(hlds[0].d), sr = Acc.HL_speedRet(hlds[0].d);
                hlSched = Mathf.Clamp01(1f - Mathf.Max(0f, speed - sd) / Mathf.Max(1f, sr - sd));
            }
            float liftGate = ground ? 1f : Mathf.Clamp01(a / 4f) * Mathf.Clamp01((nz - 0.2f) / 0.5f);
            float wingFade = ground ? 1f : 1f - Mathf.Clamp01((a - 20f) / 10f);            // gone before the wing C_L peak (35 deg)
            float manu = ground ? 0f : 8f * Mathf.Clamp01(a / 10f) * wingFade * liftGate;   // maneuver flap, baseline 8 deg
            float hlWant = hlSched * liftGate * wingFade * sched;
            float want = 15f * hlWant + manu;

            // stall cap from the flaperon model: own local AoA incl. the TE-down control deflection the roll
            // axis may need (larger of what it is using now and what the roll stick could ask for)
            float cap = 99f;
            float teDownCtl = 0f;
            foreach (Drive d in drives)
            {
                if (!d.flap || d.surf == null || !d.surf.active) continue;
                teDownCtl = Mathf.Max(teDownCtl, Mathf.Max(0f, -d.surf.cmd));
            }
            float headroom = Mathf.Max(teDownCtl, 12f * Mathf.Abs(stickRoll));
            foreach (Drive d in drives)
            {
                if (!d.flap || d.surf == null || !d.surf.active || !d.Functional) continue;
                float biasNow = -(d.angle + (d.hl != null ? d.hlAngle * d.hl.pos : 0f));
                float clean = d.surf.localAoANow - biasNow + d.surf.now;           // local AoA with zero bias and zero control
                cap = Mathf.Min(cap, d.surf.stallAoA - 5f - clean - headroom);
            }
            if (ground) cap = 99f;
            float target = Mathf.Clamp(Mathf.Min(want, cap), 0f, 23f);
            flapTarget = target;
            flapBias = Mathf.MoveTowards(flapBias, target, (target > flapBias ? 20f : 60f) * dt);

            // high-lift device: speed-scheduled part, never more than the total bias allows
            float posT = Mathf.Min(hlWant, flapBias / 15f);
            foreach (Hld h in hlds)
            {
                if (h.d == null) continue;
                h.pos = Mathf.MoveTowards(h.pos, Mathf.Clamp01(posT), dt);        // same 1/s slew as the device
                Acc.HL_position(h.d) = h.pos;
                if (h.part != null) Acc.AP_wingArea(h.part) = Mathf.Lerp(Acc.HL_areaRet(h.d), Acc.HL_areaDep(h.d), h.pos);
                foreach (MovingPart mp in h.parts) mp.Animate(h.pos);
                hlPos = h.pos;
            }

            // ---------------- outer flaperons: droop with the flaps, TE-up load relief ----------------
            // Outer flaperons droop with the flap schedule (60 % of the inboard bias), capped by their own
            // local-AoA stall margin with the roll headroom they may need, like the inboard ones.
            float ailWant = Plugin.OuterFlapShare * want;
            float ailCap = 99f, ailTeDown = 0f;
            foreach (Drive d in drives)
                if (!d.flap && d.surf != null && d.surf.active) ailTeDown = Mathf.Max(ailTeDown, Mathf.Max(0f, -d.surf.cmd));
            float ailHead = Mathf.Max(ailTeDown, 18f * Mathf.Abs(stickRoll));
            foreach (Drive d in drives)
            {
                if (d.flap || d.surf == null || !d.surf.active || !d.Functional) continue;
                float clean = d.surf.localAoANow + d.angle + d.surf.now;          // drive angle + = TE up (less AoA)
                ailCap = Mathf.Min(ailCap, d.surf.stallAoA - 5f - clean - ailHead);
            }
            if (ground) ailCap = 99f;
            float ailTarget = Mathf.Clamp(Mathf.Min(ailWant, ailCap), 0f, 15f);
            ailBias = Mathf.MoveTowards(ailBias, ailTarget, (ailTarget > ailBias ? 15f : 40f) * dt);
            relief = ground ? 0f : 9.6f * Mathf.Clamp01((nz - 3f) / 6f) * (1f - Mathf.Clamp01((a - 16f) / 8f));

            float rate = 70f * Plugin.ActuatorScale * dt;
            foreach (Drive d in drives)
            {
                if (d.rotator == null || !d.Functional) continue;
                float goal = d.flap ? -flapBias - (d.hl != null ? d.hlAngle * d.hl.pos : 0f) : relief - ailBias;
                goal = Mathf.Clamp(goal, -25f, 20f);
                // load relief moves slowly (it shifts the pitching moment with load factor: a fast drive would
                // feed back into the load loop at high dynamic pressure); flaps use the servo rate
                d.angle = Mathf.MoveTowards(d.angle, goal, d.flap ? rate : Mathf.Min(rate, 12f * dt));
                d.rotator.localRotation = d.baseRot * Quaternion.Euler(d.angle, 0f, 0f);
                if (fDeflection != null) fDeflection.SetValue(d.resp, d.angle);
            }
        }

        /// <summary>Hand the devices back to their own scripts (continuous: they resume from here).</summary>
        public void Release() { Active = false; }
    }
}
