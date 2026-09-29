using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Aryx_F22E_StrikeRaptor.FCS
{
	internal static class Countermeasures
	{
		private static readonly AccessTools.FieldRef<Countermeasure, int> CM_ammo = AccessTools.FieldRefAccess<Countermeasure, int>("ammo");

		private static readonly AccessTools.FieldRef<FlareEjector, int> FE_maxAmmo = AccessTools.FieldRefAccess<FlareEjector, int>("maxAmmo");

		private static readonly AccessTools.FieldRef<PowerSupply, float> PS_charge = AccessTools.FieldRefAccess<PowerSupply, float>("charge");

		private static readonly AccessTools.FieldRef<PowerSupply, float> PS_maxCharge = AccessTools.FieldRefAccess<PowerSupply, float>("maxCharge");

		private static readonly AccessTools.FieldRef<PowerSupply, float> PS_chargePerRPM = AccessTools.FieldRefAccess<PowerSupply, float>("chargePerRPM");

		private static readonly HashSet<int> done = new HashSet<int>();

		private static readonly Dictionary<int, int> attempts = new Dictionary<int, int>();

		private static readonly Dictionary<int, float> nextTry = new Dictionary<int, float>();

		private static readonly HashSet<int> flaresScaled = new HashSet<int>();

		private static readonly HashSet<int> suppliesScaled = new HashSet<int>();

		private static readonly HashSet<int> gunsScaled = new HashSet<int>();

		private static readonly HashSet<int> f22 = new HashSet<int>();

		private static readonly AccessTools.FieldRef<Gun, int> G_magCap = AccessTools.FieldRefAccess<Gun, int>("magazineCapacity");

		private static readonly AccessTools.FieldRef<Gun, int> G_maxMags = AccessTools.FieldRefAccess<Gun, int>("maxMagazines");

		private static readonly AccessTools.FieldRef<Weapon, WeaponStation> W_station = AccessTools.FieldRefAccess<Weapon, WeaponStation>("weaponStation");

		public static void Ensure(Aircraft a)
		{
			if ((UnityEngine.Object)(object)a == null)
			{
				return;
			}
			int instanceID = ((UnityEngine.Object)(object)a).GetInstanceID();
			if (done.Contains(instanceID))
			{
				return;
			}
			f22.Add(instanceID);
			if (Plugin.FlareCountScale == 1f && Plugin.EWCapacityScale == 1f && Plugin.EWRechargeScale == 1f && Plugin.GunAmmoScale == 1f)
			{
				done.Add(instanceID);
				return;
			}
			float time = Time.time;
			if (nextTry.TryGetValue(instanceID, out var value) && time < value)
			{
				return;
			}
			nextTry[instanceID] = time + 0.5f;
			try
			{
				int num = 0;
				int num2 = 0;
				int num3 = 0;
				foreach (FlareEjector item in Acc.Collect<FlareEjector>(a))
				{
					num++;
					if (!flaresScaled.Add(item.GetInstanceID()))
					{
						continue;
					}
					int num4 = FE_maxAmmo(item);
					if (num4 <= 0)
					{
						num4 = CM_ammo(item);
					}
					int num5 = Math.Max(num4, Mathf.RoundToInt((float)num4 * Plugin.FlareCountScale));
					int num6 = CM_ammo(item);
					CM_ammo(item) = ((num6 >= num4) ? num5 : Math.Min(num5, Mathf.RoundToInt((float)num6 * Plugin.FlareCountScale)));
					FE_maxAmmo(item) = num5;
					num2 += num4;
					num3 += num5;
					try
					{
						if (GameManager.IsLocalAircraft(a))
						{
							item.UpdateHUD();
						}
					}
					catch
					{
					}
				}
				bool flag = false;
				PowerSupply powerSupply = null;
				try
				{
					powerSupply = a.GetPowerSupply();
				}
				catch
				{
				}
				string text = "no power supply";
				if (powerSupply != null)
				{
					flag = true;
					if (suppliesScaled.Add(powerSupply.GetInstanceID()))
					{
						float num7 = PS_maxCharge(powerSupply);
						float num8 = PS_charge(powerSupply);
						float num9 = PS_chargePerRPM(powerSupply);
						PS_maxCharge(powerSupply) = num7 * Plugin.EWCapacityScale;
						if (num8 >= num7 - 0.001f && num7 > 0f)
						{
							PS_charge(powerSupply) = PS_maxCharge(powerSupply);
						}
						PS_chargePerRPM(powerSupply) = num9 * Plugin.EWRechargeScale;
						text = $"EW storage {num7:F0} -> {PS_maxCharge(powerSupply):F0}, recharge/RPM {num9:G4} -> {PS_chargePerRPM(powerSupply):G4}";
					}
					else
					{
						text = "EW already scaled";
					}
				}
				int num10 = 0;
				bool flag2 = Plugin.GunAmmoScale == 1f;
				string text2 = "";
				if (!flag2)
				{
					flag2 = true;
					HashSet<WeaponStation> hashSet = new HashSet<WeaponStation>();
					foreach (Gun item2 in Acc.Collect<Gun>(a))
					{
						if (!(item2 == null))
						{
							num10++;
							if (gunsScaled.Add(item2.GetInstanceID()))
							{
								int num11 = G_magCap(item2);
								G_magCap(item2) = Math.Max(num11, Mathf.RoundToInt((float)num11 * Plugin.GunAmmoScale));
								text2 += $" gun {item2.name}: magazine {num11} -> {G_magCap(item2)} (x{1 + G_maxMags(item2)})";
							}
							WeaponStation weaponStation = W_station(item2);
							if (weaponStation == null)
							{
								flag2 = false;
							}
							else
							{
								hashSet.Add(weaponStation);
							}
						}
					}
					if (num10 == 0)
					{
						flag2 = false;
					}
					foreach (WeaponStation item3 in hashSet)
					{
						int num12 = 0;
						foreach (Weapon weapon in item3.Weapons)
						{
							if (weapon != null)
							{
								num12 += weapon.GetFullAmmo();
							}
						}
						int ammoTotal = item3.GetAmmoTotal();
						if (num12 > ammoTotal)
						{
							item3.Rearm(num12 - ammoTotal);
							text2 += $"; station topped up {ammoTotal} -> {item3.GetAmmoTotal()}";
						}
					}
				}
				int value2;
				int num13 = ((!attempts.TryGetValue(instanceID, out value2)) ? 1 : (value2 + 1));
				attempts[instanceID] = num13;
				bool flag3 = num > 0 && flag && flag2;
				if (flag3 || num13 >= 60)
				{
					done.Add(instanceID);
					attempts.Remove(instanceID);
					nextTry.Remove(instanceID);
					Plugin.Log.LogInfo(string.Format("F-22E countermeasures / ammo on {0}: {1} flare ejector(s), capacity {2} -> {3}; {4};{5}{6}.", ((UnityEngine.Object)(object)a).name, num, num2, num3, text, (num10 == 0) ? " no gun found" : text2, flag3 ? "" : " (incomplete after 30 s, giving up)"));
				}
			}
			catch (Exception ex)
			{
				done.Add(instanceID);
				Plugin.Log.LogError("F-22E countermeasure setup failed: " + ex.Message);
			}
		}

		public static void UseFuelPrefix(Aircraft __instance, ref float __0)
		{
			try
			{
				if ((UnityEngine.Object)(object)__instance != null && Plugin.FuelUseScale != 1f && f22.Contains(((UnityEngine.Object)(object)__instance).GetInstanceID()))
				{
					__0 *= Mathf.Max(0f, Plugin.FuelUseScale);
				}
			}
			catch
			{
			}
		}

		public static void ModifyCapacitancePrefix(PowerSupply __instance, ref float capacitance)
		{
			try
			{
				if (__instance != null && suppliesScaled.Contains(__instance.GetInstanceID()))
				{
					capacitance *= Plugin.EWCapacityScale;
				}
			}
			catch
			{
			}
		}
	}
}
