using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using NuclearOption.Jobs;
using UnityEngine;

namespace Aryx_F22E_StrikeRaptor.FCS
{
	internal static class Hooks
	{
		public static readonly Type VentType = AccessTools.TypeByName("Aryx_F22E_StrikeRaptor.Aryx_OverpressureVentController");

		private static readonly FieldInfo fVentAircraft = ((VentType != null) ? AccessTools.Field(VentType, "aircraft") : null);

		private static bool aimValid;

		private static GlobalPosition aimDest;

		private static bool aimVelocityCtx;

		private static bool aimMovingCtx;

		public static void AutoAimPrefix(GlobalPosition destination, bool aimVelocity, Vector3 targetVel)
		{
			aimValid = true;
			aimDest = destination;
			aimVelocityCtx = aimVelocity;
			aimMovingCtx = targetVel.sqrMagnitude > 900f;
		}

		public static void AutoAimPostfix()
		{
			aimValid = false;
		}

		private static float GunRollFade(Aircraft aircraft, FcsController c)
		{
			c.aiGunTrack = false;
			if (!aimValid || aimVelocityCtx || !aimMovingCtx || (UnityEngine.Object)(object)aircraft.cockpit == null)
			{
				return 1f;
			}
			c.aiGunTrack = true;
			if (!Plugin.AIGunRollFade)
			{
				return 1f;
			}
			Vector3 vector = aircraft.cockpit.xform.forward;
			WeaponStation weaponStation = ((aircraft.weaponManager != null) ? aircraft.weaponManager.currentWeaponStation : null);
			if (weaponStation != null && weaponStation.WeaponInfo != null && weaponStation.WeaponInfo.gun)
			{
				foreach (Weapon weapon in weaponStation.Weapons)
				{
					if ((UnityEngine.Object)(object)weapon != null)
					{
						vector += weapon.transform.forward * 1000f;
					}
				}
			}
			float num = Vector3.Angle(vector, aimDest - aircraft.GlobalPosition());
			return Mathf.Clamp01((num - GunFadeStart) / GunFadeSpan);
		}

		internal static float GunFadeStart = 1f;

		internal static float GunFadeSpan = 5f;

		public static bool FilterPrefix(ControlsFilter __instance, ControlInputs inputs, Vector3 rawInputs, Rigidbody rb, float gForce, bool flightAssist)
		{
			Aircraft aircraft = Acc.CF_aircraft(__instance);
			if (!Plugin.Enabled || (UnityEngine.Object)(object)aircraft == null || !Registry.IsF22(aircraft))
			{
				return true;
			}
			bool flag = (UnityEngine.Object)(object)aircraft.Player == null;
			Registry.NoteHook(aircraft, flag);
			if (!aircraft.LocalSim || (flag && !Plugin.AIFlightControl))
			{
				Registry.Disengage(aircraft);
				return true;
			}
			FcsController orCreate = Registry.GetOrCreate(aircraft);
			if (orCreate == null || orCreate.Failed)
			{
				return true;
			}
			try
			{
				if (orCreate.isAI != flag)
				{
					orCreate.isAI = flag;
					if (flag)
					{
						Registry.ReadStockRateGain(__instance, orCreate);
					}
				}
				if (flag)
				{
					float fade = GunRollFade(aircraft, orCreate);
					orCreate.aiRollFade = fade;
					inputs.roll *= fade;
				}
				orCreate.Tick(inputs, flag || flightAssist);
				return false;
			}
			catch (Exception e)
			{
				Registry.Fail(orCreate, e);
				return true;
			}
		}

		// 1.8.36 nose-wheel FBW: LandingGear.FixedUpdate turns the steerable wheel toward controlInputs.yaw x steeringLock at
		// steeringSpeed. For the F-22E's steering leg the FCS's wheel command is swapped in for the pedal during that call
		// and the pilot's pedal restored afterwards (the main legs' differential brake keeps reading the real pedal).
		public static void GearPrefix(LandingGear __instance, out float __state)
		{
			__state = float.NaN;
			try
			{
				if (!Plugin.Enabled || !Acc.LG_steering(__instance))
				{
					return;
				}
				Aircraft aircraft = Acc.LG_aircraft(__instance);
				if ((UnityEngine.Object)(object)aircraft == null || !Registry.IsF22(aircraft))
				{
					return;
				}
				FcsController fcsController = Registry.Find(aircraft);
				if (fcsController == null || fcsController.Failed || !fcsController.NoseSteerOverride(out var yaw))
				{
					return;
				}
				ControlInputs controlInputs = Acc.LG_inputs(__instance);
				if (controlInputs != null)
				{
					__state = controlInputs.yaw;
					controlInputs.yaw = yaw;
				}
			}
			catch (Exception)
			{
				__state = float.NaN;
			}
		}

		public static void GearPostfix(LandingGear __instance, float __state)
		{
			if (float.IsNaN(__state))
			{
				return;
			}
			try
			{
				ControlInputs controlInputs = Acc.LG_inputs(__instance);
				if (controlInputs != null)
				{
					controlInputs.yaw = __state;
				}
			}
			catch (Exception)
			{
			}
		}

		public static void SurfacePostfix(ControlSurface __instance)
		{
			try
			{
				Aircraft aircraft = Acc.CS_aircraft(__instance);
				if ((UnityEngine.Object)(object)aircraft != null && Registry.IsF22(aircraft))
				{
					Registry.EnsureSetup(aircraft);
				}
				if (!Acc.JobCreated(__instance))
				{
					return;
				}
				ref ControlSurfaceFields reference = ref Acc.Job(__instance);
				if ((UnityEngine.Object)(object)aircraft != null && Registry.IsF22(aircraft))
				{
					reference.servoSpeed = Acc.CS_servo(__instance);
				}
				FcsController fcsController = Registry.Find(aircraft) ?? Registry.FindAnySurface(__instance);
				if (fcsController != null && fcsController.Engaged && !fcsController.Failed)
				{
					reference.brakeRange = 0f;
					reference.controlInputs.brake = 0f;
					reference.controlInputs.throttle = (fcsController.SplitDeploy ? 0f : 1f);
				}
				if (!Registry.TrySurface(__instance, out var s, out var c))
				{
					return;
				}
				if (c.Engaged && !c.Failed && s.active && !Acc.CS_locked(__instance))
				{
					if (!s.overriding)
					{
						float currentPitch = reference.currentPitch + reference.currentRoll + reference.currentYaw;
						reference.currentPitch = currentPitch;
						reference.currentRoll = 0f;
						reference.currentYaw = 0f;
						s.overriding = true;
					}
					reference.pitchRange = s.Span;
					reference.rollRange = 0f;
					reference.yawRange = 0f;
					reference.controlInputs.pitch = Mathf.Clamp(s.cmdOut / s.Span, -1f, 1f);
					reference.controlInputs.roll = 0f;
					reference.controlInputs.yaw = 0f;
				}
				else if (s.overriding)
				{
					reference.pitchRange = s.origPitchRange;
					reference.rollRange = s.origRollRange;
					reference.yawRange = s.origYawRange;
					reference.brakeRange = Acc.CS_brakeRange(__instance);
					s.overriding = false;
					if (c.Failed)
					{
						Registry.ForgetSurface(__instance);
					}
				}
			}
			catch (Exception ex)
			{
				Plugin.Log.LogError("F-22E surface hook: " + ex.Message);
			}
		}

		public static void VentPostfix(MonoBehaviour __instance, ref float __result)
		{
			try
			{
				string mode = Plugin.VentDoors;
				if (mode == "Stock" || fVentAircraft == null || __instance == null)
				{
					return;
				}
				Aircraft a = fVentAircraft.GetValue(__instance) as Aircraft;
				FcsController c = Registry.Find(a);
				if (c == null)
				{
					return;
				}
				if (mode == "Airbrake")
				{
					if (c.Engaged && !c.Failed)
					{
						__result = Mathf.Clamp01(c.airBrake);
					}
					return;
				}
				__result = BypassDoorTarget(c);
			}
			catch
			{
			}
		}

		// 1.8.37: the six upper-fuselage gills behave as inlet bypass doors. They dump the inlet air the
		// engines don't take: captured airflow grows with Mach, engine demand with spool, so they open at
		// speed with the throttle back, close as the engines spool up, and close at high AoA (the inlet
		// captures less). No brake input.
		internal static float BypassDoorTarget(FcsController c)
		{
			Aircraft a = c.ac;
			if (a == null)
			{
				return 0f;
			}
			float rho = Mathf.Clamp(a.airDensity, 0.05f, 1.4f);
			float h = (1f - Mathf.Pow(rho / 1.225f, 0.23496f)) / 2.2558e-5f;
			float T = Mathf.Max(216.65f, 288.15f - 0.0065f * h);
			float mach = c.tas / Mathf.Sqrt(401.87f * T);
			float demand = 0f;
			int n = 0;
			foreach (Turbofan e in c.tvc.engines)
			{
				if (e == null)
				{
					continue;
				}
				float r = 0f;
				try
				{
					r = Mathf.Clamp01(e.GetRPMRatio());
				}
				catch
				{
				}
				demand += (r < 0.25f) ? 0f : r;
				n++;
			}
			demand = (n > 0) ? (demand / n) : 0f;
			float open0 = 0.35f + 0.95f * demand;
			float x = Mathf.Clamp01((mach - open0) / 0.45f);
			float aoa = 1f - Mathf.InverseLerp(12f, 24f, Mathf.Abs(c.alpha));
			return Mathf.SmoothStep(0f, 1f, x) * aoa;
		}

		public static bool FlapDevicePrefix(MonoBehaviour __instance)
		{
			try
			{
				if (__instance != null && Registry.TryDevice(__instance, out var c) && c.Engaged && !c.Failed && c.flaps.Active)
				{
					return false;
				}
			}
			catch
			{
			}
			return true;
		}

		public static void TurbofanPrefix(Turbofan __instance)
		{
			Aircraft aircraft = Acc.TF_aircraft(__instance);
			if ((UnityEngine.Object)(object)aircraft != null && Registry.IsF22(aircraft))
			{
				Registry.EnsureSetup(aircraft);
			}
		}

		public static float TvcPitch(ControlInputs ci)
		{
			if (ci != null && Registry.TryInputs(ci, out var c) && c.Engaged && !c.Failed)
			{
				return c.tvcPitchInput;
			}
			return ci?.pitch ?? 0f;
		}

		public static float NozzleSlew(Turbofan t)
		{
			if (t == null)
			{
				return 70f;
			}
			Aircraft aircraft = Acc.TF_aircraft(t);
			if (!((UnityEngine.Object)(object)aircraft != null) || !Registry.IsF22(aircraft))
			{
				return 70f;
			}
			return Plugin.TvcSlew;
		}

		public static float NozzleSlewNeg(Turbofan t)
		{
			return 0f - NozzleSlew(t);
		}

		public static IEnumerable<CodeInstruction> TurbofanTranspiler(IEnumerable<CodeInstruction> instructions)
		{
			FieldInfo fPitch = AccessTools.Field(typeof(ControlInputs), "pitch");
			MethodInfo mTvc = AccessTools.Method(typeof(Hooks), "TvcPitch");
			MethodInfo mSlew = AccessTools.Method(typeof(Hooks), "NozzleSlew");
			MethodInfo mSlewNeg = AccessTools.Method(typeof(Hooks), "NozzleSlewNeg");
			int nPitch = 0;
			int nSlew = 0;
			foreach (CodeInstruction instruction in instructions)
			{
				if (instruction.opcode == OpCodes.Ldfld && instruction.operand is FieldInfo fieldInfo && fieldInfo == fPitch)
				{
					CodeInstruction codeInstruction = new CodeInstruction(OpCodes.Call, mTvc);
					codeInstruction.labels.AddRange(instruction.labels);
					codeInstruction.blocks.AddRange(instruction.blocks);
					nPitch++;
					yield return codeInstruction;
				}
				else if (instruction.opcode == OpCodes.Ldc_R4 && instruction.operand is float fv && (fv == 70f || fv == -70f))
				{
					CodeInstruction codeInstruction2 = new CodeInstruction(OpCodes.Ldarg_0);
					codeInstruction2.labels.AddRange(instruction.labels);
					codeInstruction2.blocks.AddRange(instruction.blocks);
					yield return codeInstruction2;
					yield return new CodeInstruction(OpCodes.Call, (fv > 0f) ? mSlew : mSlewNeg);
					nSlew++;
				}
				else
				{
					yield return instruction;
				}
			}
			Plugin.Log.LogInfo($"Turbofan transpiler: {nPitch} pitch read(s) routed to FCS, {nSlew} nozzle slew constant(s) scaled.");
		}
	}
}
