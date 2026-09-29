using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Aryx_F22E_StrikeRaptor.FCS
{
	internal static class Registry
	{
		private static readonly Dictionary<int, bool> isF22 = new Dictionary<int, bool>();

		private static readonly HashSet<int> setupDone = new HashSet<int>();

		private static readonly Dictionary<int, FcsController> controllers = new Dictionary<int, FcsController>();

		private static readonly Dictionary<int, SurfaceEffector> surfaceMap = new Dictionary<int, SurfaceEffector>();

		private static readonly Dictionary<int, FcsController> surfaceOwner = new Dictionary<int, FcsController>();

		private static readonly Dictionary<int, FcsController> anySurfaceOwner = new Dictionary<int, FcsController>();

		private static readonly Dictionary<ControlInputs, FcsController> byInputs = new Dictionary<ControlInputs, FcsController>();

		private static readonly HashSet<int> failed = new HashSet<int>();

		private static readonly Dictionary<int, int> attachRetry = new Dictionary<int, int>();

		private static readonly HashSet<int> tvcFixed = new HashSet<int>();

		private static readonly HashSet<int> servoScaled = new HashSet<int>();

		private static readonly HashSet<int> areaScaled = new HashSet<int>();

		private static readonly Dictionary<int, float> setupRetry = new Dictionary<int, float>();

		private static readonly HashSet<int> dragScaled = new HashSet<int>();

		private static readonly HashSet<int> cgShifted = new HashSet<int>();

		private static readonly Dictionary<int, FcsController> deviceOwner = new Dictionary<int, FcsController>();

		private static readonly FieldInfo fAfterburners = AccessTools.Field(typeof(JetNozzle), "afterburners");

		public static FcsController Find(Aircraft a)
		{
			if (!((UnityEngine.Object)(object)a != null) || !controllers.TryGetValue(((UnityEngine.Object)(object)a).GetInstanceID(), out var value))
			{
				return null;
			}
			return value;
		}

		public static FcsController FindAnySurface(ControlSurface cs)
		{
			if (!(cs != null) || !anySurfaceOwner.TryGetValue(cs.GetInstanceID(), out var value))
			{
				return null;
			}
			return value;
		}

		public static bool IsF22(Aircraft a)
		{
			if ((UnityEngine.Object)(object)a == null)
			{
				return false;
			}
			int instanceID = ((UnityEngine.Object)(object)a).GetInstanceID();
			if (isF22.TryGetValue(instanceID, out var value))
			{
				return value;
			}
			value = ((Component)(object)a).gameObject.name.StartsWith("Aryx_KingRaptor", StringComparison.Ordinal);
			if (!value)
			{
				try
				{
					AircraftParameters aircraftParameters = a.GetAircraftParameters();
					value = aircraftParameters != null && aircraftParameters.aircraftName != null && aircraftParameters.aircraftName.Contains("Strike Raptor");
				}
				catch
				{
					value = false;
				}
			}
			isF22[instanceID] = value;
			return value;
		}

		private static void ScaleAfterburners(JetNozzle n, float k)
		{
			if (n == null || fAfterburners == null || !(fAfterburners.GetValue(n) is Array array))
			{
				return;
			}
			foreach (object item in array)
			{
				if (item != null)
				{
					Traverse traverse = Traverse.Create(item).Field("thrust");
					traverse.SetValue(traverse.GetValue<float>() * k);
				}
			}
		}

		public static bool TryDevice(Component d, out FcsController c)
		{
			return deviceOwner.TryGetValue(d.GetInstanceID(), out c);
		}

		public static void EnsureSetup(Aircraft a)
		{
			if ((UnityEngine.Object)(object)a == null)
			{
				return;
			}
			Countermeasures.Ensure(a);
			int instanceID = ((UnityEngine.Object)(object)a).GetInstanceID();
			if (setupDone.Contains(instanceID))
			{
				return;
			}
			float time = Time.time;
			if (setupRetry.TryGetValue(instanceID, out var value) && time < value)
			{
				return;
			}
			setupRetry[instanceID] = time + 0.5f;
			try
			{
				int num = 0;
				int num2 = 0;
				int num3 = 0;
				int num4 = 0;
				foreach (Turbofan item in Acc.Collect<Turbofan>(a))
				{
					num++;
					if (!tvcFixed.Add(item.GetInstanceID()))
					{
						continue;
					}
					try
					{
						NozzleRig.Attach(item, a);
					}
					catch (Exception ex0)
					{
						Plugin.Log.LogWarning("F-22E nozzle rig: " + ex0.Message);
					}
					Acc.TF_vec(item) = new Vector3((Plugin.TvcRange > 0f) ? Plugin.TvcRange : Acc.TF_vec(item).x, 0f, 0f);
					Acc.TF_staticThrust(item) *= Plugin.ThrustScale;
					JetNozzle[] array = Acc.TF_nozzles(item);
					if (array != null)
					{
						JetNozzle[] array2 = array;
						for (int i = 0; i < array2.Length; i++)
						{
							ScaleAfterburners(array2[i], Plugin.ThrustScale);
						}
					}
				}
				foreach (UnitPart item2 in a.partLookup)
				{
					if (item2 is AeroPart aeroPart && dragScaled.Add(aeroPart.GetInstanceID()))
					{
						Acc.AP_dragArea(aeroPart) *= Plugin.DragScale;
						num4++;
					}
				}
				foreach (UnitPart item3 in a.partLookup)
				{
					if (item3 is AeroPart aeroPart2 && item3.gameObject.name.Contains("LERX") && Mathf.Abs(Plugin.LerxEffectiveness - 1f) > 0.0001f && areaScaled.Add(aeroPart2.GetInstanceID()))
					{
						Acc.AP_wingArea(aeroPart2) *= Plugin.LerxEffectiveness;
					}
				}
				if (Plugin.CgShiftAft > 0.0001f && !cgShifted.Contains(instanceID))
				{
					UnitPart unitPart = null;
					UnitPart unitPart2 = null;
					float num5 = 0f;
					foreach (UnitPart item4 in a.partLookup)
					{
						if (!(item4 == null))
						{
							num5 += item4.mass;
							if (item4.gameObject.name.EndsWith("_Fore"))
							{
								unitPart = item4;
							}
							else if (item4.gameObject.name.EndsWith("_Back"))
							{
								unitPart2 = item4;
							}
						}
					}
					if (unitPart != null && unitPart2 != null && num5 > 1000f)
					{
						Transform transform = ((Component)(object)a).transform;
						float num6 = Vector3.Dot(unitPart.transform.position - unitPart2.transform.position, transform.forward);
						float num7 = Mathf.Min(Plugin.CgShiftAft * num5 / Mathf.Max(1f, num6), 0.6f * unitPart.mass);
						unitPart.mass -= num7;
						unitPart2.mass += num7;
						if (unitPart.rb != null && unitPart.rb != a.rb)
						{
							unitPart.rb.mass = unitPart.mass;
						}
						if (unitPart2.rb != null && unitPart2.rb != a.rb)
						{
							unitPart2.rb.mass = unitPart2.mass;
						}
						cgShifted.Add(instanceID);
						Plugin.Log.LogInfo($"F-22E CG shift: {num7:F0} kg moved {unitPart.gameObject.name} -> {unitPart2.gameObject.name} ({num6:F2} m) = {num7 * num6 / num5:F3} m aft of {num5:F0} kg");
					}
				}
				foreach (ControlSurface item5 in Acc.Collect<ControlSurface>(a))
				{
					num2++;
					if (servoScaled.Add(item5.GetInstanceID()))
					{
						Acc.CS_servo(item5) *= Plugin.ActuatorScale;
					}
					AeroPart aeroPart3 = Acc.CS_attached(item5) as AeroPart;
					if (!(aeroPart3 == null))
					{
						string name = item5.gameObject.name;
						float num8 = 1f;
						if (Mathf.Abs(Acc.CS_pitchRange(item5)) > 0.01f || name.Contains("Elevator"))
						{
							num8 = Plugin.StabilatorEffectiveness;
						}
						else if (name.Contains("Flap") || name.Contains("Aileron"))
						{
							num8 = Plugin.FlaperonEffectiveness;
						}
						if (Mathf.Abs(num8 - 1f) > 0.0001f && areaScaled.Add(aeroPart3.GetInstanceID()))
						{
							Acc.AP_wingArea(aeroPart3) *= num8;
							num3++;
						}
					}
				}
				bool flag = num2 >= 6 && num >= 2;
				if (flag)
				{
					setupDone.Add(instanceID);
					setupRetry.Remove(instanceID);
					try
					{
						DragRelief.Attach(a);
					}
					catch (Exception exD)
					{
						Plugin.Log.LogWarning("F-22E drag relief: " + exD.Message);
					}
				}
				Plugin.Log.LogInfo(string.Format("F-22E airframe setup on {0}: {1} engines (roll/yaw TVC removed, thrust x{2}), {3} surfaces (servo x{4}), {5} newly effectiveness-scaled, {6} parts drag x{7}{8}.", ((UnityEngine.Object)(object)a).name, num, Plugin.ThrustScale, num2, Plugin.ActuatorScale, num3, num4, Plugin.DragScale, flag ? "" : " - incomplete, will retry"));
			}
			catch (Exception ex)
			{
				Plugin.Log.LogError("F-22E airframe setup failed: " + ex);
			}
		}

		private static float nextPrune;

		private static readonly Dictionary<int, int> hookState = new Dictionary<int, int>();

		public static void NoteHook(Aircraft a, bool ai)
		{
			try
			{
				int num = (a.LocalSim ? (ai ? (Plugin.AIFlightControl ? 1 : 2) : 0) : 3);
				int instanceID = ((UnityEngine.Object)(object)a).GetInstanceID();
				if (hookState.TryGetValue(instanceID, out var value) && value == num)
				{
					return;
				}
				hookState[instanceID] = num;
				string text = num switch
				{
					0 => "human pilot on this machine -> full FCS (AoA/G command)", 
					1 => "AI pilot on this machine -> FCS with AI pitch-rate command", 
					2 => "AI pilot on this machine -> stock fly-by-wire (AIFlightControl = false)", 
					_ => "simulated on another machine -> left to that machine", 
				};
				Plugin.Log.LogInfo($"F-22E FCS: jet #{instanceID} reached the flight-control hook: {text}.");
			}
			catch
			{
			}
		}

		private static readonly FieldInfo fFbw = AccessTools.Field(typeof(ControlsFilter), "flyByWire");

		private static readonly FieldInfo fFbwG = ((fFbw != null) ? AccessTools.Field(fFbw.FieldType, "gLimitPositive") : null);

		private static readonly FieldInfo fFbwCorner = ((fFbw != null) ? AccessTools.Field(fFbw.FieldType, "cornerSpeed") : null);

		public static void ReadStockRateGain(ControlsFilter f, FcsController c)
		{
			try
			{
				object obj = ((f != null && fFbw != null) ? fFbw.GetValue(f) : null);
				if (obj != null)
				{
					if (fFbwG != null)
					{
						c.aiGLimit = Mathf.Clamp((float)fFbwG.GetValue(obj), 3f, 15f);
					}
					if (fFbwCorner != null)
					{
						c.aiCornerSpeed = Mathf.Clamp((float)fFbwCorner.GetValue(obj), 50f, 400f);
					}
				}
				Plugin.Log.LogInfo($"F-22E FCS flying an AI F-22E: pitch-rate command, stock stick gain ({c.aiGLimit:0.#} g, corner {c.aiCornerSpeed:0} m/s), FCS AoA/G limits.");
			}
			catch (Exception ex)
			{
				Plugin.Log.LogWarning("F-22E FCS: could not read the stock fly-by-wire gain for an AI jet, using 9.5 g / 140 m/s: " + ex.Message);
			}
		}

		private static void Prune()
		{
			float time = Time.time;
			if (time < nextPrune && time >= nextPrune - 10f)
			{
				return;
			}
			nextPrune = time + 5f;
			List<int> list = null;
			foreach (KeyValuePair<int, FcsController> controller in controllers)
			{
				if ((UnityEngine.Object)(object)controller.Value.ac == null)
				{
					(list ?? (list = new List<int>())).Add(controller.Key);
				}
			}
			if (list == null)
			{
				return;
			}
			foreach (int item in list)
			{
				FcsController c = controllers[item];
				controllers.Remove(item);
				try
				{
					c.Dispose();
				}
				catch
				{
				}
				foreach (KeyValuePair<int, FcsController> item3 in new List<KeyValuePair<int, FcsController>>(surfaceOwner))
				{
					if (item3.Value == c)
					{
						surfaceOwner.Remove(item3.Key);
						surfaceMap.Remove(item3.Key);
					}
				}
				RemoveFrom(anySurfaceOwner, (FcsController o) => o == c);
				RemoveFrom(deviceOwner, (FcsController o) => o == c);
				List<ControlInputs> list2 = new List<ControlInputs>();
				foreach (KeyValuePair<ControlInputs, FcsController> byInput in byInputs)
				{
					if (byInput.Value == c)
					{
						list2.Add(byInput.Key);
					}
				}
				foreach (ControlInputs item2 in list2)
				{
					byInputs.Remove(item2);
				}
			}
		}

		private static void RemoveFrom<T>(Dictionary<int, T> d, Func<T, bool> match)
		{
			List<int> list = new List<int>();
			foreach (KeyValuePair<int, T> item in d)
			{
				if (match(item.Value))
				{
					list.Add(item.Key);
				}
			}
			foreach (int item2 in list)
			{
				d.Remove(item2);
			}
		}

		public static FcsController GetOrCreate(Aircraft a)
		{
			Prune();
			int instanceID = ((UnityEngine.Object)(object)a).GetInstanceID();
			if (controllers.TryGetValue(instanceID, out var value))
			{
				return value;
			}
			if (failed.Contains(instanceID))
			{
				return null;
			}
			try
			{
				EnsureSetup(a);
				value = new FcsController(a);
				controllers[instanceID] = value;
				foreach (SurfaceEffector surface in value.surfaces)
				{
					surfaceMap[surface.cs.GetInstanceID()] = surface;
					surfaceOwner[surface.cs.GetInstanceID()] = value;
				}
				List<ControlSurface> list = Acc.Collect<ControlSurface>(a);
				List<string> list2 = new List<string>();
				foreach (ControlSurface item in list)
				{
					if (!(item == null))
					{
						anySurfaceOwner[item.GetInstanceID()] = value;
						float num = Acc.CS_maxSplit(item);
						float num2 = Acc.CS_brakeRange(item);
						bool flag = surfaceMap.ContainsKey(item.GetInstanceID());
						if (num > 0f || Mathf.Abs(num2) > 0.01f)
						{
							list2.Add(string.Format("{0}{1}: maxSplit {2:F1}, brakeRange {3:F1}", item.gameObject.name.Replace("Aryx_KingRaptor_", ""), flag ? "" : " [not an FCS effector]", num, num2));
						}
					}
				}
				Plugin.Log.LogInfo((list2.Count > 0) ? string.Format("F-22E FCS airbrake inventory ({0} control surfaces): {1} - the FCS drives these instead of the stock brake/idle-throttle rules.", list.Count, string.Join("; ", list2.ToArray())) : $"F-22E FCS airbrake inventory ({list.Count} control surfaces): none of them split or respond to the brake input, so whatever deploys is not a ControlSurface.");
				foreach (int ownedId in value.flaps.ownedIds)
				{
					deviceOwner[ownedId] = value;
				}
				if (value.inputs != null)
				{
					byInputs[value.inputs] = value;
				}
				return value;
			}
			catch (InvalidOperationException ex)
			{
				if (!attachRetry.TryGetValue(instanceID, out var value2))
				{
					value2 = 0;
				}
				value2 = (attachRetry[instanceID] = value2 + 1);
				if (value2 == 1 || value2 % 250 == 0)
				{
					Plugin.Log.LogWarning("F-22E FCS waiting to attach (attempt " + value2 + "): " + ex.Message);
				}
				if (value2 > 1500)
				{
					failed.Add(instanceID);
					Plugin.Log.LogError("F-22E FCS gave up attaching; stock flight controls stay active: " + ex.Message);
				}
				return null;
			}
			catch (Exception ex2)
			{
				failed.Add(instanceID);
				Plugin.Log.LogError("F-22E FCS could not attach; stock flight controls stay active for this aircraft: " + ex2);
				return null;
			}
		}

		public static void Fail(FcsController c, Exception e)
		{
			Plugin.Log.LogError("F-22E FCS fault, reverting this aircraft to stock flight controls: " + e);
			c.Failed = true;
			c.Engaged = false;
			if ((UnityEngine.Object)(object)c.ac != null)
			{
				failed.Add(((UnityEngine.Object)(object)c.ac).GetInstanceID());
			}
			Remove(c);
		}

		public static void Disengage(Aircraft a)
		{
			if (!((UnityEngine.Object)(object)a == null) && controllers.TryGetValue(((UnityEngine.Object)(object)a).GetInstanceID(), out var value))
			{
				value.Engaged = false;
			}
		}

		public static void Remove(FcsController c)
		{
			c.Dispose();
			if ((UnityEngine.Object)(object)c.ac != null)
			{
				controllers.Remove(((UnityEngine.Object)(object)c.ac).GetInstanceID());
			}
			if (c.inputs != null)
			{
				byInputs.Remove(c.inputs);
			}
			foreach (int ownedId in c.flaps.ownedIds)
			{
				deviceOwner.Remove(ownedId);
			}
		}

		public static bool TrySurface(ControlSurface cs, out SurfaceEffector s, out FcsController c)
		{
			int instanceID = cs.GetInstanceID();
			s = null;
			c = null;
			if (surfaceMap.TryGetValue(instanceID, out s))
			{
				return surfaceOwner.TryGetValue(instanceID, out c);
			}
			return false;
		}

		public static void ForgetSurface(ControlSurface cs)
		{
			int instanceID = cs.GetInstanceID();
			surfaceMap.Remove(instanceID);
			surfaceOwner.Remove(instanceID);
		}

		public static bool TryInputs(ControlInputs ci, out FcsController c)
		{
			return byInputs.TryGetValue(ci, out c);
		}
	}
}
