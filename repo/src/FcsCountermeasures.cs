// Countermeasure changes for every F-22E (AI included, like the thrust / airframe changes):
//  - flares: FlareEjector capacity (and the load at spawn) x FlareCountScale
//  - EW: the aircraft PowerSupply's storage (maxCharge, which the radar jammer's capacitance is added to) x
//    EWCapacityScale, and its recharge (chargePerRPM, engine-driven) x EWRechargeScale.
// The jammer's own draw (powerUsage) and intensity are untouched, so the extra capacity is extra jamming time.
using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Aryx_F22E_StrikeRaptor.FCS
{
    internal static class Countermeasures
    {
        static readonly AccessTools.FieldRef<Countermeasure, int> CM_ammo = AccessTools.FieldRefAccess<Countermeasure, int>("ammo");
        static readonly AccessTools.FieldRef<FlareEjector, int> FE_maxAmmo = AccessTools.FieldRefAccess<FlareEjector, int>("maxAmmo");
        static readonly AccessTools.FieldRef<PowerSupply, float> PS_charge = AccessTools.FieldRefAccess<PowerSupply, float>("charge");
        static readonly AccessTools.FieldRef<PowerSupply, float> PS_maxCharge = AccessTools.FieldRefAccess<PowerSupply, float>("maxCharge");
        static readonly AccessTools.FieldRef<PowerSupply, float> PS_chargePerRPM = AccessTools.FieldRefAccess<PowerSupply, float>("chargePerRPM");

        static readonly HashSet<int> done = new HashSet<int>();
        static readonly Dictionary<int, int> attempts = new Dictionary<int, int>();
        static readonly Dictionary<int, float> nextTry = new Dictionary<int, float>();
        static readonly HashSet<int> flaresScaled = new HashSet<int>();
        static readonly HashSet<int> suppliesScaled = new HashSet<int>();
        static readonly HashSet<int> gunsScaled = new HashSet<int>();
        static readonly HashSet<int> f22 = new HashSet<int>();
        static readonly AccessTools.FieldRef<Gun, int> G_magCap = AccessTools.FieldRefAccess<Gun, int>("magazineCapacity");
        static readonly AccessTools.FieldRef<Gun, int> G_maxMags = AccessTools.FieldRefAccess<Gun, int>("maxMagazines");
        static readonly AccessTools.FieldRef<Weapon, WeaponStation> W_station = AccessTools.FieldRefAccess<Weapon, WeaponStation>("weaponStation");

        public static void Ensure(Aircraft a)
        {
            if (a == null) return;
            int id = a.GetInstanceID();
            if (done.Contains(id)) return;
            // all scales at 1 (stock-performance build default): leave the game's countermeasures completely untouched
            f22.Add(id);   // fuel-consumption scaling applies to every F-22E that went through setup
            if (Plugin.FlareCountScale == 1f && Plugin.EWCapacityScale == 1f && Plugin.EWRechargeScale == 1f && Plugin.GunAmmoScale == 1f) { done.Add(id); return; }
            float now = Time.time;
            if (nextTry.TryGetValue(id, out float t) && now < t) return;
            nextTry[id] = now + 0.5f;
            try
            {
                int nFlare = 0, ammoOld = 0, ammoNew = 0;
                foreach (FlareEjector fe in Acc.Collect<FlareEjector>(a))
                {
                    nFlare++;
                    if (!flaresScaled.Add(fe.GetInstanceID())) continue;
                    int maxOld = FE_maxAmmo(fe);
                    if (maxOld <= 0) maxOld = CM_ammo(fe);      // Awake not run yet: it copies ammo into maxAmmo
                    int maxNew = Math.Max(maxOld, Mathf.RoundToInt(maxOld * Plugin.FlareCountScale));
                    int cur = CM_ammo(fe);
                    CM_ammo(fe) = cur >= maxOld ? maxNew : Math.Min(maxNew, Mathf.RoundToInt(cur * Plugin.FlareCountScale));
                    FE_maxAmmo(fe) = maxNew;
                    ammoOld += maxOld; ammoNew += maxNew;
                    try { if (GameManager.IsLocalAircraft(a)) fe.UpdateHUD(); } catch { }
                }

                bool haveSupply = false;
                PowerSupply ps = null;
                try { ps = a.GetPowerSupply(); } catch { }
                string ew = "no power supply";
                if (ps != null)
                {
                    haveSupply = true;
                    if (suppliesScaled.Add(ps.GetInstanceID()))
                    {
                        float max0 = PS_maxCharge(ps), c0 = PS_charge(ps), r0 = PS_chargePerRPM(ps);
                        PS_maxCharge(ps) = max0 * Plugin.EWCapacityScale;
                        if (c0 >= max0 - 1e-3f && max0 > 0f) PS_charge(ps) = PS_maxCharge(ps);   // was full: stays full
                        PS_chargePerRPM(ps) = r0 * Plugin.EWRechargeScale;
                        ew = $"EW storage {max0:F0} -> {PS_maxCharge(ps):F0}, recharge/RPM {r0:G4} -> {PS_chargePerRPM(ps):G4}";
                    }
                    else ew = "EW already scaled";
                }

                // gun ammunition: magazine capacity x GunAmmoScale (what the game refills to, at spawn and at base),
                // then the station is topped up through the game's own rearm path (hardpoint mass, HUD, sync)
                int nGun = 0; bool gunsDone = Plugin.GunAmmoScale == 1f; string gunTxt = "";
                if (!gunsDone)
                {
                    gunsDone = true;
                    var stations = new HashSet<WeaponStation>();
                    foreach (Gun g in Acc.Collect<Gun>(a))
                    {
                        if (g == null) continue;
                        nGun++;
                        if (gunsScaled.Add(g.GetInstanceID()))
                        {
                            int cap0 = G_magCap(g);
                            G_magCap(g) = Math.Max(cap0, Mathf.RoundToInt(cap0 * Plugin.GunAmmoScale));
                            gunTxt += $" gun {g.name}: magazine {cap0} -> {G_magCap(g)} (x{1 + G_maxMags(g)})";
                        }
                        WeaponStation ws = W_station(g);
                        if (ws == null) { gunsDone = false; continue; }   // not registered to a station yet: retry
                        stations.Add(ws);
                    }
                    if (nGun == 0) gunsDone = false;
                    foreach (WeaponStation ws in stations)
                    {
                        int full = 0; foreach (Weapon w in ws.Weapons) if (w != null) full += w.GetFullAmmo();
                        int have = ws.GetAmmoTotal();
                        if (full > have) { ws.Rearm(full - have); gunTxt += $"; station topped up {have} -> {ws.GetAmmoTotal()}"; }
                    }
                }

                int n = attempts.TryGetValue(id, out int k) ? k + 1 : 1;
                attempts[id] = n;
                bool complete = nFlare > 0 && haveSupply && gunsDone;
                if (complete || n >= 60)
                {
                    done.Add(id); attempts.Remove(id); nextTry.Remove(id);
                    Plugin.Log.LogInfo($"F-22E countermeasures / ammo on {a.name}: {nFlare} flare ejector(s), capacity {ammoOld} -> {ammoNew}; {ew};{(nGun == 0 ? " no gun found" : gunTxt)}{(complete ? "" : " (incomplete after 30 s, giving up)")}.");
                }
            }
            catch (Exception e) { done.Add(id); Plugin.Log.LogError("F-22E countermeasure setup failed: " + e.Message); }
        }

        /// <summary>Fuel burn of every engine (dry and afterburner) goes through Aircraft.UseFuel(amount): scaled for F-22Es.</summary>
        public static void UseFuelPrefix(Aircraft __instance, ref float __0)
        {
            try { if (__instance != null && Plugin.FuelUseScale != 1f && f22.Contains(__instance.GetInstanceID())) __0 *= Mathf.Max(0f, Plugin.FuelUseScale); }
            catch { }
        }

        /// <summary>Capacitance added later (a jammer attaching after setup) is scaled the same way.</summary>
        public static void ModifyCapacitancePrefix(PowerSupply __instance, ref float capacitance)
        {
            try { if (__instance != null && suppliesScaled.Contains(__instance.GetInstanceID())) capacitance *= Plugin.EWCapacityScale; }
            catch { }
        }
    }
}
