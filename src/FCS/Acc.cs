using System;
using System.Collections.Generic;
using HarmonyLib;
using NuclearOption.Jobs;
using UnityEngine;

namespace Aryx_F22E_StrikeRaptor.FCS
{
	internal static class Acc
	{
		public static readonly AccessTools.FieldRef<LandingGear, Aircraft> LG_aircraft = AccessTools.FieldRefAccess<LandingGear, Aircraft>("aircraft");

		public static readonly AccessTools.FieldRef<LandingGear, ControlInputs> LG_inputs = AccessTools.FieldRefAccess<LandingGear, ControlInputs>("controlInputs");

		public static readonly AccessTools.FieldRef<LandingGear, bool> LG_steering = AccessTools.FieldRefAccess<LandingGear, bool>("steering");

		public static readonly AccessTools.FieldRef<LandingGear, float> LG_steerLock = AccessTools.FieldRefAccess<LandingGear, float>("steeringLock");

		public static readonly AccessTools.FieldRef<LandingGear, float> LG_steerAngle = AccessTools.FieldRefAccess<LandingGear, float>("steeringAngle");

		public static readonly AccessTools.FieldRef<AeroPart, int> AP_airfoil = AccessTools.FieldRefAccess<AeroPart, int>("airfoil");

		public static readonly AccessTools.FieldRef<AeroPart, Vector3> AP_centerOfLift = AccessTools.FieldRefAccess<AeroPart, Vector3>("centerOfLift");

		public static readonly AccessTools.FieldRef<AeroPart, Transform> AP_liftNormal = AccessTools.FieldRefAccess<AeroPart, Transform>("liftNormal");

		public static readonly AccessTools.FieldRef<AeroPart, float> AP_wingArea = AccessTools.FieldRefAccess<AeroPart, float>("wingArea");

		public static readonly AccessTools.FieldRef<AeroPart, float> AP_wingEff = AccessTools.FieldRefAccess<AeroPart, float>("wingEffectiveness");

		public static readonly AccessTools.FieldRef<Aircraft, List<ControlSurface>> AC_surfaces = AccessTools.FieldRefAccess<Aircraft, List<ControlSurface>>("controlSurfaces");

		public static readonly AccessTools.FieldRef<Aircraft, Vector3> AC_wind = AccessTools.FieldRefAccess<Aircraft, Vector3>("windVelocity");

		public static readonly AccessTools.FieldRef<ControlSurface, PtrAllocation<ControlSurfaceFields>> CS_job = AccessTools.FieldRefAccess<ControlSurface, PtrAllocation<ControlSurfaceFields>>("JobFields");

		public static readonly AccessTools.FieldRef<ControlSurface, Aircraft> CS_aircraft = AccessTools.FieldRefAccess<ControlSurface, Aircraft>("aircraft");

		public static readonly AccessTools.FieldRef<ControlSurface, UnitPart> CS_attached = AccessTools.FieldRefAccess<ControlSurface, UnitPart>("attachedSurface");

		public static readonly AccessTools.FieldRef<ControlSurface, float> CS_pitchRange = AccessTools.FieldRefAccess<ControlSurface, float>("pitchRange");

		public static readonly AccessTools.FieldRef<ControlSurface, float> CS_rollRange = AccessTools.FieldRefAccess<ControlSurface, float>("rollRange");

		public static readonly AccessTools.FieldRef<ControlSurface, float> CS_yawRange = AccessTools.FieldRefAccess<ControlSurface, float>("yawRange");

		public static readonly AccessTools.FieldRef<ControlSurface, float> CS_brakeRange = AccessTools.FieldRefAccess<ControlSurface, float>("brakeRange");

		public static readonly AccessTools.FieldRef<ControlSurface, float> CS_maxSplit = AccessTools.FieldRefAccess<ControlSurface, float>("maxSplit");

		public static readonly AccessTools.FieldRef<ControlSurface, float> CS_splitAmount = AccessTools.FieldRefAccess<ControlSurface, float>("splitAmount");

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

		public static readonly AccessTools.FieldRef<HighLiftDevice, float> HL_alphaMin = AccessTools.FieldRefAccess<HighLiftDevice, float>("alphaMin");

		public static readonly AccessTools.FieldRef<HighLiftDevice, float> HL_alphaFactor = AccessTools.FieldRefAccess<HighLiftDevice, float>("alphaFactor");

		public static List<Component> Collect(Aircraft a, Type t)
		{
			HashSet<int> seen = new HashSet<int>();
			List<Component> list = new List<Component>();
			if ((UnityEngine.Object)(object)a == null || t == null)
			{
				return list;
			}
			Component[] componentsInChildren = ((Component)(object)a).GetComponentsInChildren(t, includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				Add(componentsInChildren[i]);
			}
			if (a.partLookup != null)
			{
				foreach (UnitPart item in a.partLookup)
				{
					if (item != null)
					{
						componentsInChildren = item.GetComponentsInChildren(t, includeInactive: true);
						for (int i = 0; i < componentsInChildren.Length; i++)
						{
							Add(componentsInChildren[i]);
						}
					}
				}
			}
			return list;
			void Add(Component c)
			{
				if (c != null && seen.Add(c.GetInstanceID()))
				{
					list.Add(c);
				}
			}
		}

		public static List<T> Collect<T>(Aircraft a) where T : Component
		{
			HashSet<int> seen = new HashSet<int>();
			List<T> list = new List<T>();
			if ((UnityEngine.Object)(object)a == null)
			{
				return list;
			}
			T[] componentsInChildren = ((Component)(object)a).GetComponentsInChildren<T>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				Add(componentsInChildren[i]);
			}
			if (a.partLookup != null)
			{
				foreach (UnitPart item in a.partLookup)
				{
					if (item != null)
					{
						componentsInChildren = item.GetComponentsInChildren<T>(includeInactive: true);
						for (int i = 0; i < componentsInChildren.Length; i++)
						{
							Add(componentsInChildren[i]);
						}
					}
				}
			}
			if (typeof(T) == typeof(ControlSurface))
			{
				List<ControlSurface> list2 = AC_surfaces(a);
				if (list2 != null)
				{
					foreach (ControlSurface item2 in list2)
					{
						Add(item2 as T);
					}
				}
			}
			return list;
			void Add(T c)
			{
				if (c != null && seen.Add(c.GetInstanceID()))
				{
					list.Add(c);
				}
			}
		}

		public static bool JobCreated(ControlSurface cs)
		{
			return CS_job(cs).IsCreated;
		}

		public static ref ControlSurfaceFields Job(ControlSurface cs)
		{
			return ref CS_job(cs).Ref();
		}
	}
}
