// Accessors for non-public game fields. Mono enforces field visibility at JIT time, so these go through
// Harmony's FieldRefAccess (DynamicMethod with skipVisibility) instead of a publicized reference.
using HarmonyLib;
using UnityEngine;
using NuclearOption.Jobs;

namespace Aryx_F22E_StrikeRaptor.FCS
{
    internal static class Acc
    {
        public static readonly AccessTools.FieldRef<AeroPart, int> AP_airfoil = AccessTools.FieldRefAccess<AeroPart, int>("airfoil");
        public static readonly AccessTools.FieldRef<AeroPart, Vector3> AP_centerOfLift = AccessTools.FieldRefAccess<AeroPart, Vector3>("centerOfLift");
        public static readonly AccessTools.FieldRef<AeroPart, Transform> AP_liftNormal = AccessTools.FieldRefAccess<AeroPart, Transform>("liftNormal");
        public static readonly AccessTools.FieldRef<AeroPart, float> AP_wingArea = AccessTools.FieldRefAccess<AeroPart, float>("wingArea");
        public static readonly AccessTools.FieldRef<AeroPart, float> AP_wingEff = AccessTools.FieldRefAccess<AeroPart, float>("wingEffectiveness");

        public static readonly AccessTools.FieldRef<Aircraft, System.Collections.Generic.List<ControlSurface>> AC_surfaces =
            AccessTools.FieldRefAccess<Aircraft, System.Collections.Generic.List<ControlSurface>>("controlSurfaces");
        public static readonly AccessTools.FieldRef<Aircraft, Vector3> AC_wind = AccessTools.FieldRefAccess<Aircraft, Vector3>("windVelocity");

        public static readonly AccessTools.FieldRef<ControlSurface, PtrAllocation<ControlSurfaceFields>> CS_job =
            AccessTools.FieldRefAccess<ControlSurface, PtrAllocation<ControlSurfaceFields>>("JobFields");
        public static readonly AccessTools.FieldRef<ControlSurface, Aircraft> CS_aircraft = AccessTools.FieldRefAccess<ControlSurface, Aircraft>("aircraft");
        public static readonly AccessTools.FieldRef<ControlSurface, UnitPart> CS_attached = AccessTools.FieldRefAccess<ControlSurface, UnitPart>("attachedSurface");
        public static readonly AccessTools.FieldRef<ControlSurface, float> CS_pitchRange = AccessTools.FieldRefAccess<ControlSurface, float>("pitchRange");
        public static readonly AccessTools.FieldRef<ControlSurface, float> CS_rollRange = AccessTools.FieldRefAccess<ControlSurface, float>("rollRange");
        public static readonly AccessTools.FieldRef<ControlSurface, float> CS_yawRange = AccessTools.FieldRefAccess<ControlSurface, float>("yawRange");
        public static readonly AccessTools.FieldRef<ControlSurface, float> CS_servo = AccessTools.FieldRefAccess<ControlSurface, float>("servoSpeed");
        public static readonly AccessTools.FieldRef<ControlSurface, bool> CS_locked = AccessTools.FieldRefAccess<ControlSurface, bool>("locked");
        public static readonly AccessTools.FieldRef<ControlSurface, GameObject> CS_visible = AccessTools.FieldRefAccess<ControlSurface, GameObject>("visibleMesh");

        public static readonly AccessTools.FieldRef<ControlsFilter, Aircraft> CF_aircraft = AccessTools.FieldRefAccess<ControlsFilter, Aircraft>("aircraft");

        public static readonly AccessTools.FieldRef<JetNozzle, Transform> JN_thrustT = AccessTools.FieldRefAccess<JetNozzle, Transform>("thrustTransform");
        public static readonly AccessTools.FieldRef<JetNozzle, float> JN_totalThrust = AccessTools.FieldRefAccess<JetNozzle, float>("totalThrust");

        public static readonly AccessTools.FieldRef<Turbofan, Aircraft> TF_aircraft = AccessTools.FieldRefAccess<Turbofan, Aircraft>("aircraft");
        public static readonly AccessTools.FieldRef<Turbofan, Vector3> TF_nozzleAngles = AccessTools.FieldRefAccess<Turbofan, Vector3>("nozzleAngles");
        public static readonly AccessTools.FieldRef<Turbofan, JetNozzle[]> TF_nozzles = AccessTools.FieldRefAccess<Turbofan, JetNozzle[]>("nozzles");
        public static readonly AccessTools.FieldRef<Turbofan, bool> TF_operable = AccessTools.FieldRefAccess<Turbofan, bool>("operable");
        public static readonly AccessTools.FieldRef<Turbofan, Vector3> TF_vec = AccessTools.FieldRefAccess<Turbofan, Vector3>("thrustVectoring");
        public static readonly AccessTools.FieldRef<Turbofan, Vector3> TF_vecGain = AccessTools.FieldRefAccess<Turbofan, Vector3>("thrustVectoringGain");
        public static readonly AccessTools.FieldRef<Turbofan, float> TF_vecMaxSpeed = AccessTools.FieldRefAccess<Turbofan, float>("thrustVectoringMaxAirspeed");
        public static readonly AccessTools.FieldRef<Turbofan, Transform[]> TF_vecT = AccessTools.FieldRefAccess<Turbofan, Transform[]>("vectoringTransforms");

        public static readonly AccessTools.FieldRef<AeroPart, float> AP_dragArea = AccessTools.FieldRefAccess<AeroPart, float>("dragArea");
        public static readonly AccessTools.FieldRef<Turbofan, float> TF_staticThrust = AccessTools.FieldRefAccess<Turbofan, float>("staticThrust");

        public static readonly AccessTools.FieldRef<HighLiftDevice, AeroPart> HL_part = AccessTools.FieldRefAccess<HighLiftDevice, AeroPart>("aeroPart");
        public static readonly AccessTools.FieldRef<HighLiftDevice, float> HL_speedDep = AccessTools.FieldRefAccess<HighLiftDevice, float>("speedDeployed");
        public static readonly AccessTools.FieldRef<HighLiftDevice, float> HL_speedRet = AccessTools.FieldRefAccess<HighLiftDevice, float>("speedRetracted");
        public static readonly AccessTools.FieldRef<HighLiftDevice, float> HL_areaDep = AccessTools.FieldRefAccess<HighLiftDevice, float>("partAreaDeployed");
        public static readonly AccessTools.FieldRef<HighLiftDevice, float> HL_areaRet = AccessTools.FieldRefAccess<HighLiftDevice, float>("partAreaRetracted");
        public static readonly AccessTools.FieldRef<HighLiftDevice, float> HL_position = AccessTools.FieldRefAccess<HighLiftDevice, float>("position");

        /// <summary>Non-generic variant of Collect for types only known by name at run time.</summary>
        public static System.Collections.Generic.List<Component> Collect(Aircraft a, System.Type t)
        {
            var seen = new System.Collections.Generic.HashSet<int>();
            var list = new System.Collections.Generic.List<Component>();
            if (a == null || t == null) return list;
            void Add(Component c) { if (c != null && seen.Add(c.GetInstanceID())) list.Add(c); }
            foreach (Component c in a.GetComponentsInChildren(t, true)) Add(c);
            if (a.partLookup != null)
                foreach (UnitPart up in a.partLookup)
                    if (up != null) foreach (Component c in up.GetComponentsInChildren(t, true)) Add(c);
            return list;
        }

        /// <summary>
        /// All components of type T belonging to the aircraft. Nuclear Option un-parents every part into its own
        /// rigidbody object when complex physics starts (AeroPart.CreateRB -> SetParent), so a hierarchy search
        /// from the aircraft root misses almost everything: walk every part in partLookup instead.
        /// </summary>
        public static System.Collections.Generic.List<T> Collect<T>(Aircraft a) where T : Component
        {
            var seen = new System.Collections.Generic.HashSet<int>();
            var list = new System.Collections.Generic.List<T>();
            void Add(T c) { if (c != null && seen.Add(c.GetInstanceID())) list.Add(c); }
            if (a == null) return list;
            foreach (T c in a.GetComponentsInChildren<T>(true)) Add(c);
            if (a.partLookup != null)
                foreach (UnitPart up in a.partLookup)
                    if (up != null) foreach (T c in up.GetComponentsInChildren<T>(true)) Add(c);
            if (typeof(T) == typeof(ControlSurface))
            {
                var l = AC_surfaces(a);
                if (l != null) foreach (ControlSurface cs in l) Add(cs as T);
            }
            return list;
        }

        public static bool JobCreated(ControlSurface cs) => CS_job(cs).IsCreated;
        public static ref ControlSurfaceFields Job(ControlSurface cs) => ref CS_job(cs).Ref();
    }
}
