// F-22E FCS — plugin entry, configuration, Harmony hooks, per-aircraft setup.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using NuclearOption.Jobs;

[assembly: System.Runtime.CompilerServices.IgnoresAccessChecksTo("Assembly-CSharp")]

namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal sealed class IgnoresAccessChecksToAttribute : Attribute
    {
        public IgnoresAccessChecksToAttribute(string assemblyName) { AssemblyName = assemblyName; }
        public string AssemblyName { get; }
    }
}

#if STANDALONE
namespace Aryx_F22E_StrikeRaptor.FCS
{
    /// <summary>Companion build: FCS only, runs alongside the unmodified Aryx 1.0.4 mod DLL.</summary>
#if STOCKPERF
    // Stock-performance variant: same plugin GUID (BepInEx refuses to load it next to the buffed build), own config
    // file so the two builds' performance defaults never mix.
    [BepInPlugin("Aryx_F22E_StrikeRaptor.FCS", "F-22E Strike Raptor FCS (stock performance)", "1.7.6")]
#else
    [BepInPlugin("Aryx_F22E_StrikeRaptor.FCS", "F-22E Strike Raptor FCS", "1.7.6")]
#endif
    [BepInDependency("Aryx_F22E_StrikeRaptor", BepInDependency.DependencyFlags.SoftDependency)]
    public class FcsStandalonePlugin : BaseUnityPlugin
    {
        private void Awake()
        {
#if STOCKPERF
            var cfg = new ConfigFile(System.IO.Path.Combine(Paths.ConfigPath, "Aryx_F22E_StrikeRaptor.FCS.StockPerf.cfg"), true);
            Plugin.Init(cfg, Logger);
            Logger.LogInfo("Stock-performance build: original thrust, drag and countermeasures; FCS and airframe control tweaks only.");
#else
            Plugin.Init(Config, Logger);
#endif
            Plugin.ApplyPatches(new Harmony("com.aryx.strikeraptor.fcs"));
        }
    }
}
#else
namespace Aryx_F22E_StrikeRaptor
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;

        private void Awake()
        {
            Logger = base.Logger;
            Logger.LogInfo("Aryx Dynamics Aryx_F22E_StrikeRaptor loaded. Await blueprinter start.");
            FCS.Plugin.Init(Config, Logger);
            var harmony = new Harmony("com.aryx.strikeraptor");
            harmony.CreateClassProcessor(typeof(WeaponMountInitializePatch)).Patch();
            FCS.Plugin.ApplyPatches(harmony);
        }
    }
}
#endif

namespace Aryx_F22E_StrikeRaptor.FCS
{
    internal static class Plugin
    {
        internal static ManualLogSource Log;

        static ConfigEntry<bool> cEnabled, cTelemetry;
        static ConfigEntry<int> cVersion;
        static ConfigEntry<float> cAoAPos, cAoANeg, cGPos, cGNeg, cMpoGPos, cMpoGNeg, cMaxPitch, cProt, cMaxRoll, cMaxYaw,
            cCouple, cPedalBeta, cKBeta, cKRoll, cKPitch, cKYaw, cKpAoA, cKiAoA, cKpG, cKiG, cFlaperonEff, cStabEff, cActuator,
            cRudStart, cRudEnd, cUnloadDecel, cUnloadRise, cRollLo, cRollHi, cRollAuth, cRollBrake, cStabTrim, cLerxEff, cCgShift, cCoupFF, cOnset, cStabRoll, cStabRollShare, cStickSmooth, cActDamp, cYawStop, cOuterFlap, cRollAcc, cThrust, cDrag, cYawPri, cYawFF, cRollLead, cFlareScale, cEwCap, cEwRate, cLowYaw, cLowYawIas, cLowYawFall, cTvcMove, cRollTvc, cRollWash, cGunAmmo, cFuelUse, cRotRate;
        const int ConfigVersion = 12;

        public static bool Enabled => cEnabled.Value;
        public static bool TelemetryEnabled => cTelemetry.Value;
        public static float AoAPos => cAoAPos.Value;
        public static float AoANeg => -Mathf.Abs(cAoANeg.Value);
        public static float GPos => cGPos.Value;
        public static float GNeg => -Mathf.Abs(cGNeg.Value);
        public static float MpoGPos => cMpoGPos.Value;
        public static float MpoGNeg => -Mathf.Abs(cMpoGNeg.Value);
        public static float MaxPitchRate => cMaxPitch.Value;
        public static float RotationRate => cRotRate.Value;
        public static float ProtRate => cProt.Value;
        public static float MaxRollRate => cMaxRoll.Value;
        public static float MaxYawRate => cMaxYaw.Value;
        public static float RollGainLowIAS => cRollLo.Value;
        public static float RollGainHighIAS => cRollHi.Value;
        public static float RollAuthorityUse => cRollAuth.Value;
        public static float RollBrakeUse => cRollBrake.Value;
        public static float StabTrimCost => cStabTrim.Value;
        public static float LerxEffectiveness => cLerxEff.Value;
        public static float CgShiftAft => cCgShift.Value;
        public static float CouplingFF => cCoupFF.Value;
        public static float PitchOnsetGps => cOnset.Value;
        public static float StabRollCost => cStabRoll.Value;
        public static float StickSmoothing => cStickSmooth.Value;
        public static float ActuatorDamping => cActDamp.Value;
        public static float YawStopFactor => cYawStop.Value;
        public static float LowIasYawBoost => cLowYaw.Value;
        public static float LowIasYawBoostFullIas => cLowYawIas.Value;
        public static float LowIasYawBoostFalloff => cLowYawFall.Value;
        public static float TvcMoveCost => cTvcMove.Value;
        public static float RollNozzleHold => cRollTvc.Value;
        public static float RollNozzleWashout => cRollWash.Value;
        public static float YawPriority => cYawPri.Value;
        public static float RollLead => cRollLead.Value;
        public static float YawRateFF => cYawFF.Value;
        public static float OuterFlapShare => cOuterFlap.Value;
        public static float StabRollShare => cStabRollShare.Value;
        public static float CoupleAoA => cCouple.Value;
        public static float PedalBeta => cPedalBeta.Value;
        public static float KBeta => cKBeta.Value;
        public static float KRoll => cKRoll.Value;
        public static float KPitch => cKPitch.Value;
        public static float KYaw => cKYaw.Value;
        public static float KpAoA => cKpAoA.Value;
        public static float KiAoA => cKiAoA.Value;
        public static float RudderFadeStart => cRudStart.Value;
        public static float RudderFadeEnd => cRudEnd.Value;
        public static float UnloadDecel => cUnloadDecel.Value;
        public static float UnloadRiseGain => cUnloadRise.Value;
        public static float MaxRollAccel => cRollAcc.Value;
        public static float ThrustScale => cThrust.Value;
        public static float FlareCountScale => cFlareScale.Value;
        public static float EWCapacityScale => cEwCap.Value;
        public static float EWRechargeScale => cEwRate.Value;
        public static float GunAmmoScale => cGunAmmo.Value;
        public static float FuelUseScale => cFuelUse.Value;
        public static float DragScale => cDrag.Value;
        public static float KpG => cKpG.Value;
        public static float KiG => cKiG.Value;
        public static float FlaperonEffectiveness => cFlaperonEff.Value;
        public static float StabilatorEffectiveness => cStabEff.Value;
        public static float ActuatorScale => cActuator.Value;

        public static void Init(ConfigFile cfg, ManualLogSource log)
        {
            Log = log;
            const string G = "1. General", E = "2. Envelope", L = "3. Lateral-directional", K = "4. Gains", A = "5. Airframe";
            cEnabled = cfg.Bind(G, "Enabled", true, "Replace the stock fly-by-wire with the F-22E FCS (player aircraft only).");
            cTelemetry = cfg.Bind(G, "Telemetry", false, "Write a 50 Hz CSV of the FCS state to BepInEx/F22E_FCS_<time>.csv.");
            cAoAPos = cfg.Bind(E, "AoALimitPositive", 65f, "AoA limit (deg) in AoA/G command and gear-down rate command.");
            cAoANeg = cfg.Bind(E, "AoALimitNegative", 45f, "Negative AoA limit magnitude (deg).");
            cGPos = cfg.Bind(E, "GLimitPositive", 9.5f, "Positive load limit (g), normal law.");
            cGNeg = cfg.Bind(E, "GLimitNegative", 3.5f, "Negative load limit magnitude (g), normal law.");
            cMpoGPos = cfg.Bind(E, "MPOGLimitPositive", 12.5f, "Positive load limit (g) with Stability Assist OFF (MPO).");
            cMpoGNeg = cfg.Bind(E, "MPOGLimitNegative", 3.5f, "Negative load limit magnitude (g) in MPO.");
            cMaxPitch = cfg.Bind(E, "MaxPitchRate", 48f, "Pitch-rate budget ceiling (deg/s).");
            cProt = cfg.Bind(E, "AoARecoveryRate", 48f, "AoA-protection recovery rate (deg/s), independent of thrust and weight.");
            cMaxRoll = cfg.Bind(L, "MaxRollRate", 250f, "Absolute body roll-rate ceiling (deg/s). Below ~250 m/s IAS the original-aircraft schedule and RollAuthorityUse set the rate; above it this ceiling holds the rate flat.");
            cMaxYaw = cfg.Bind(L, "MaxYawRate", 75f, "Body yaw-rate limit (deg/s).");
            cRollLo = cfg.Bind(L, "RollRateGainLowIAS", 1.20f, "Roll-rate ceiling vs the ORIGINAL aircraft's roll rate below ~120 m/s IAS (blends to the high-IAS gain by 170 m/s).");
            cRollHi = cfg.Bind(L, "RollRateGainHighIAS", 1.10f, "Roll-rate ceiling vs the original aircraft's roll rate above ~170 m/s IAS.");
            cRollBrake = cfg.Bind(L, "RollBrakeUse", 0.8f, "Fraction of roll authority used to stop a roll (roll damping helps, so more than the entry value is safe).");
            cRollAuth = cfg.Bind(L, "RollAuthorityUse", 1.0f, "Steady roll never needs more than this fraction of the available roll control moment (roll rate is given up instead of saturating the surfaces).");
            cCouple = cfg.Bind(L, "CoupledAxisAoA", 36f, "Above this |AoA| the pedals blend into the stick's velocity-vector roll (full coupling 14 deg higher).");
            cPedalBeta = cfg.Bind(L, "PedalSideslip", 15f, "Sideslip commanded by full pedal below the coupling AoA (deg).");
            cRudStart = cfg.Bind(L, "RudderFadeStartAoA", 35f, "Rudders start fading out above this |AoA| (deg) - vertical-tail blanking, as on the real F-22.");
            cRudEnd = cfg.Bind(L, "RudderFadeEndAoA", 50f, "Rudders fully faded (held neutral) above this |AoA| (deg).");
            cRollAcc = cfg.Bind(L, "MaxRollAccel", 720f, "Ceiling on the shaped velocity-vector roll acceleration (deg/s^2); below it the measured authority sets the rate.");
            cKBeta = cfg.Bind(K, "SideslipGain", 1.3f, "Stability-axis yaw rate per degree of sideslip error (1/s).");
            cKRoll = cfg.Bind(K, "RollRateGain", 6f, "Roll-rate loop bandwidth (1/s).");
            cKPitch = cfg.Bind(K, "PitchRateGain", 5f, "Pitch-rate loop bandwidth (1/s).");
            cKYaw = cfg.Bind(K, "YawRateGain", 2.5f, "Yaw-rate loop bandwidth (1/s).");
            cKpAoA = cfg.Bind(K, "AoAGain", 5f, "AoA command: deg/s of pitch rate per deg of AoA error.");
            cKiAoA = cfg.Bind(K, "AoAIntegral", 3.0f, "AoA command integral gain (1/s) near the target.");
            cStickSmooth = cfg.Bind(G, "StickSmoothing", 0.06f, "Time constant (s) of the first-order smoothing on pitch/roll/pedal inputs. 0 = off.");
            cActDamp = cfg.Bind(A, "ActuatorDamping", 0.04f, "Actuator smoothing (s): time for a servo command to accelerate to full servo speed (and brake into its target on a matching curve). Servo speed is unchanged and a steady slew runs at full speed. 0 = off.");
            cYawStop = cfg.Bind(L, "YawStopFactor", 0.7f, "Above 15-25 deg AoA the body yaw rate is capped at this x measured yaw authority (so the yaw can be stopped again). Higher = more responsive moderate-AoA rolls, more sideslip on stops.");
            cRollLead = cfg.Bind(L, "RollLagCompensation", 0.008f, "Roll command lead per unit of roll damping (s per 1/s above 2.5/s): cancels the roll loop's tracking lag at high dynamic pressure so roll stops end when the command does, without a slow tail. 0 = off.");
            cYawPri = cfg.Bind(K, "YawAxisPriority", 8f, "Allocator weight of the yaw axis relative to roll below ~20 deg AoA. Higher = rudders keep the roll coordinated even when their roll side-effect costs some roll acceleration.");
            cYawFF = cfg.Bind(K, "YawRateFeedForward", 0.5f, "Fraction of the yaw-rate command's rate of change fed forward to the yaw loop at low AoA below 200 m/s IAS (blends to 1.0 by 300 m/s IAS, and by 28 deg AoA, where the body yaw is the velocity-vector roll). Lower = calmer rudders at low speed, slightly more sideslip.");
            cLowYaw = cfg.Bind(L, "LowIasYawBoost", 1.35f, "Above 15-25 deg AoA, widens the yaw-rate cap (YawStopFactor x yaw authority, and its 20 deg/s floor) by this factor at very low IAS (falling leaf). It fades out along a curve above LowIasYawBoostFullIAS (see LowIasYawBoostFalloff). 1 = off.");
            cLowYawIas = cfg.Bind(L, "LowIasYawBoostFullIAS", 48f, "IAS (m/s) up to which the full LowIasYawBoost applies.");
            cLowYawFall = cfg.Bind(L, "LowIasYawBoostFalloff", 10f, "Above LowIasYawBoostFullIAS the extra margin decays exponentially with this IAS scale (m/s): 10 = ~50 % of it left at +7 m/s, ~10 % at +23 m/s, gone by 120 m/s.");
            const float tvcMoveDefault = 2.0e-5f;
            cTvcMove = cfg.Bind(K, "TvcMoveCost", tvcMoveDefault, "Allocator cost of moving the TVC nozzles. 2e-5 = nozzles take pitch changes first; ~0.02 = stabs do the quick pitch changes and the nozzles slowly take over the sustained (trim) share. Post-stall the nozzles are always fast.");
            cRotRate = cfg.Bind(E, "RotationRate", 8f, "Takeoff / landing rotation (main wheels on the ground, nose wheel up): full stick commands this pitch rate (deg/s) and neutral stick holds the nose attitude.");
            cRollTvc = cfg.Bind(K, "RollNozzleHold", 1f, "Low-AoA rolls with no pitch command: the stabs take over the roll surfaces' pitch-moment change so the TVC stays near its pre-roll trim. Below ~150 m/s IAS the nozzles still catch the fast part and hand the sustained part to the stabs as far as the stabs have travel to spare; above it the nozzles simply hold still. 1 = full, 0 = off (nozzles trim the roll alone, 1.7.0 behaviour).");
            cRollWash = cfg.Bind(K, "RollNozzleWashout", 0.4f, "Time constant (s) separating 'fast' (nozzles) from 'sustained' (stabs) in RollNozzleHold. Lower = the stabs take over sooner.");
            cOuterFlap = cfg.Bind(A, "OuterFlaperonDroop", 0.6f, "Outer flaperons droop with the flap schedule by this fraction of the inboard flap angle (stall-capped, roll-biased).");
            cStabRoll = cfg.Bind(K, "StabilatorRollCost", 5.0e-4f, "Allocator cost of differential (roll) stabilator use at attached flow. Lower = stabs do more of the roll.");
            cStabRollShare = cfg.Bind(K, "StabilatorRollShare", 0.55f, "Share of the differential-stab roll moment counted as usable roll authority (sets roll rate ceiling / acceleration).");
            cOnset = cfg.Bind(K, "PitchOnsetLimit", 150f, "High-speed pitch pacing: pitch-rate command acceleration <= this / lift slope (g per deg). Lower = gentler at high dynamic pressure.");
            cCoupFF = cfg.Bind(K, "InertialCouplingFF", 1.0f, "Feed-forward of the gyroscopic roll/pitch/yaw coupling (w x Iw) between commanded and present rates. 0 = off.");
            const float stabTrimDefault = 0.004f;
            cStabTrim = cfg.Bind(K, "StabilatorTrimCost", stabTrimDefault, "Allocator cost of symmetric (pitch) stabilator deflection relative to the nozzles. Higher = steady trim / sustained pitch rate carried by TVC, stabs kept for transients and whatever TVC cannot hold.");
            cUnloadDecel = cfg.Bind(K, "UnloadDecel", 20f, "Easing the stick: pitch-rate deceleration (deg/s^2) toward the rate the aircraft will sustain at the new AoA/G. Lower = smoother.");
            cUnloadRise = cfg.Bind(K, "UnloadRiseGain", 1.5f, "Easing the stick: if AoA still rises during the decel, the pitch-rate target drops by this x AoA rate (to zero at most).");
            cKpG = cfg.Bind(K, "GGain", 2.5f, "G command bandwidth (1/s): load error is converted to AoA error through the live lift slope.");
            cKiG = cfg.Bind(K, "GIntegral", 1.0f, "G command integral gain (1/s^2), removes steady load error.");
            cFlaperonEff = cfg.Bind(A, "FlaperonEffectiveness", 1.50f, "Aerodynamic effectiveness multiplier on the flaperons and ailerons (scales their lifting area). Applied at spawn.");
            cStabEff = cfg.Bind(A, "StabilatorEffectiveness", 1.10f, "Aerodynamic effectiveness multiplier on the stabilators. Applied at spawn.");
            cActuator = cfg.Bind(A, "ActuatorSpeedScale", 1.30f, "Servo speed multiplier for every control surface and the TVC nozzles.");
            cLerxEff = cfg.Bind(A, "LERXEffectiveness", 1.6f, "Aerodynamic effectiveness multiplier on the LERX (lifting area). Forward lift: moves the neutral point forward (less static stability, less trim).");
            cCgShift = cfg.Bind(A, "CGShiftAft", 0.05f, "Moves the centre of mass aft by this many metres (mass moved from the forward fuselage to the aft fuselage, total mass unchanged). Main gear is ~0.3 m behind the stock CG: keep this small.");
#if STOCKPERF
            const float flareDef = 1f, ewCapDef = 1f, ewRateDef = 1f, thrustDef = 1f, dragDef = 1f, gunDef = 1f, fuelDef = 1f;
#else
            const float flareDef = 10f, ewCapDef = 1.5f, ewRateDef = 1.2f, thrustDef = 1.08f, dragDef = 0.885f, gunDef = 2f, fuelDef = 0.75f;
#endif
            const string CM = "Countermeasures (all F-22Es, applied at spawn)";
            cFlareScale = cfg.Bind(CM, "FlareCountScale", flareDef, "Flare capacity multiplier (the load at spawn and what rearming refills to).");
            cEwCap = cfg.Bind(CM, "EWCapacityScale", ewCapDef, "Electrical storage multiplier for the EW/jammer power supply (more jamming time per charge).");
            cEwRate = cfg.Bind(CM, "EWRechargeScale", ewRateDef, "Recharge-rate multiplier for the EW/jammer power supply (engine-driven charging).");
            cGunAmmo = cfg.Bind(CM, "GunAmmoScale", gunDef, "Gun (20 mm) ammunition multiplier: magazine capacity, so the load at spawn and what rearming refills to.");
            cFuelUse = cfg.Bind(A, "FuelConsumptionScale", fuelDef, "Fuel burn multiplier for every engine at every throttle setting, afterburner included (0.75 = 25 % less fuel used). All F-22Es.");
            cThrust = cfg.Bind(A, "ThrustScale", thrustDef, "Engine thrust multiplier (dry staticThrust and afterburner thrust alike, so max-AB total scales the same). Fuel flow per power setting is unchanged.");
            cDrag = cfg.Bind(A, "ParasiticDragScale", dragDef, "Multiplier on every part's parasitic drag area (1 = stock). 0.885 = about -10 % total straight-line drag (parasitic is 85-93 % of it).");
            cVersion = cfg.Bind(G, "ConfigVersion", 0, "Internal: defaults changed in newer builds are migrated once. Do not edit.");
            if (cVersion.Value < ConfigVersion)
            {
                // values whose defaults changed in 1.3.0: take the new default once
                var changed = new List<ConfigEntryBase>();
                if (cVersion.Value < 12) changed.AddRange(new ConfigEntryBase[] { cRollTvc, cStabRollShare });
                if (cVersion.Value < 11) changed.AddRange(new ConfigEntryBase[] { cStabRoll, cStabRollShare });
                if (cVersion.Value < 10) changed.Add(cLowYaw);
                if (cVersion.Value < 9) changed.AddRange(new ConfigEntryBase[] { cMaxRoll, cMaxYaw });
                if (cVersion.Value < 8) changed.AddRange(new ConfigEntryBase[] { cActDamp, cRollLead });
                if (cVersion.Value < 6) changed.AddRange(new ConfigEntryBase[] { cKBeta, cKYaw });
                if (cVersion.Value < 5) changed.Add(cStabEff);
                if (cVersion.Value < 4) changed.Add(cMaxRoll);
                if (cVersion.Value < 3) changed.AddRange(new ConfigEntryBase[] { cKBeta, cFlaperonEff, cStabEff });
                foreach (var e in changed) e.BoxedValue = e.DefaultValue;
                cVersion.Value = ConfigVersion;
                cfg.Save();
            }
        }

        public static void ApplyPatches(Harmony h)
        {
            TryPatch(h, "ControlsFilter.Filter", () => h.Patch(AccessTools.Method(typeof(ControlsFilter), "Filter"),
                prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.FilterPrefix))));
            TryPatch(h, "ControlSurface.UpdateJobFields", () => h.Patch(AccessTools.Method(typeof(ControlSurface), "UpdateJobFields"),
                postfix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.SurfacePostfix))));
            TryPatch(h, "PowerSupply.ModifyCapacitance", () => h.Patch(AccessTools.Method(typeof(PowerSupply), "ModifyCapacitance"),
                prefix: new HarmonyMethod(typeof(Countermeasures), nameof(Countermeasures.ModifyCapacitancePrefix))));
            TryPatch(h, "Aircraft.UseFuel", () => h.Patch(AccessTools.Method(typeof(Aircraft), "UseFuel", new[] { typeof(float) }),
                prefix: new HarmonyMethod(typeof(Countermeasures), nameof(Countermeasures.UseFuelPrefix))));
            TryPatch(h, "HighLiftDevice.FixedUpdate", () => h.Patch(AccessTools.Method(typeof(HighLiftDevice), "FixedUpdate"),
                prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.FlapDevicePrefix))));
            if (FlapSystem.ResponderType != null)
                TryPatch(h, "AryxAlphaResponder.FixedUpdate", () => h.Patch(AccessTools.Method(FlapSystem.ResponderType, "FixedUpdate"),
                    prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.FlapDevicePrefix))));
            else
                Log.LogWarning("AryxAlphaResponder type not found (original F-22E mod not loaded?) - flap takeover limited to the high-lift devices.");
            TryPatch(h, "Turbofan.FixedUpdate", () => h.Patch(AccessTools.Method(typeof(Turbofan), "FixedUpdate"),
                prefix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.TurbofanPrefix)),
                transpiler: new HarmonyMethod(typeof(Hooks), nameof(Hooks.TurbofanTranspiler))));
            Log.LogInfo($"F-22E FCS 1.7.6 ready (AoA +{AoAPos}/{AoANeg}, G +{GPos}/{GNeg}, MPO +{MpoGPos}/{MpoGNeg}, " +
                        $"q<= {MaxPitchRate}, p<= {MaxRollRate}, r<= {MaxYawRate}; flaperon x{FlaperonEffectiveness}, stab x{StabilatorEffectiveness}, actuators x{ActuatorScale}, thrust x{ThrustScale}, parasitic drag x{DragScale}).");
        }

        private static void TryPatch(Harmony h, string what, Action a)
        {
            try { a(); Log.LogInfo("Installed hook: " + what); }
            catch (Exception e) { Log.LogError("Failed to install hook " + what + ": " + e); }
        }
    }

    internal static class Registry
    {
        public static FcsController Find(Aircraft a) => a != null && controllers.TryGetValue(a.GetInstanceID(), out var c) ? c : null;
        static readonly Dictionary<int, bool> isF22 = new Dictionary<int, bool>();
        static readonly HashSet<int> setupDone = new HashSet<int>();
        static readonly Dictionary<int, FcsController> controllers = new Dictionary<int, FcsController>();
        static readonly Dictionary<int, SurfaceEffector> surfaceMap = new Dictionary<int, SurfaceEffector>();
        static readonly Dictionary<int, FcsController> surfaceOwner = new Dictionary<int, FcsController>();
        static readonly Dictionary<ControlInputs, FcsController> byInputs = new Dictionary<ControlInputs, FcsController>();
        static readonly HashSet<int> failed = new HashSet<int>();
        static readonly Dictionary<int, int> attachRetry = new Dictionary<int, int>();

        public static bool IsF22(Aircraft a)
        {
            if (a == null) return false;
            int id = a.GetInstanceID();
            if (isF22.TryGetValue(id, out bool v)) return v;
            v = a.gameObject.name.StartsWith("Aryx_KingRaptor", StringComparison.Ordinal);
            if (!v)
            {
                try { var p = a.GetAircraftParameters(); v = p != null && p.aircraftName != null && p.aircraftName.Contains("Strike Raptor"); }
                catch { v = false; }
            }
            isF22[id] = v;
            return v;
        }

        /// <summary>One-time airframe changes applied to every F-22E (player or AI): roll/yaw TVC removed,
        /// actuator speed scaled, flaperon/stabilator effectiveness scaled.</summary>
        static readonly HashSet<int> tvcFixed = new HashSet<int>();
        static readonly HashSet<int> servoScaled = new HashSet<int>();
        static readonly HashSet<int> areaScaled = new HashSet<int>();
        static readonly Dictionary<int, float> setupRetry = new Dictionary<int, float>();
        static readonly HashSet<int> dragScaled = new HashSet<int>();
        static readonly HashSet<int> cgShifted = new HashSet<int>();
        static readonly Dictionary<int, FcsController> deviceOwner = new Dictionary<int, FcsController>();
        static readonly FieldInfo fAfterburners = AccessTools.Field(typeof(JetNozzle), "afterburners");

        static void ScaleAfterburners(JetNozzle n, float k)
        {
            if (n == null || fAfterburners == null || !(fAfterburners.GetValue(n) is Array arr)) return;
            foreach (object ab in arr)
            {
                if (ab == null) continue;
                Traverse f = Traverse.Create(ab).Field("thrust");
                f.SetValue(f.GetValue<float>() * k);
            }
        }

        public static bool TryDevice(Component d, out FcsController c) => deviceOwner.TryGetValue(d.GetInstanceID(), out c);

        /// <summary>One-time airframe changes applied to every F-22E (player or AI): roll/yaw TVC removed,
        /// actuator speed scaled, flaperon/stabilator effectiveness scaled. Idempotent per component, and only
        /// marked complete once the surfaces and engines have actually been found (retried every 0.5 s).</summary>
        public static void EnsureSetup(Aircraft a)
        {
            if (a == null) return;
            Countermeasures.Ensure(a);
            int id = a.GetInstanceID();
            if (setupDone.Contains(id)) return;
            float now = Time.time;
            if (setupRetry.TryGetValue(id, out float next) && now < next) return;
            setupRetry[id] = now + 0.5f;
            try
            {
                int nFan = 0, nSurf = 0, nArea = 0, nDrag = 0;
                foreach (Turbofan t in Acc.Collect<Turbofan>(a))
                {
                    nFan++;
                    if (tvcFixed.Add(t.GetInstanceID()))
                    {
                        Acc.TF_vec(t) = new Vector3(Acc.TF_vec(t).x, 0f, 0f);
                        // thrust: dry and afterburner by the same factor -> max-AB total scales by it too;
                        // fuel flow (spool- and AB-amount-based) is untouched
                        Acc.TF_staticThrust(t) *= Plugin.ThrustScale;
                        JetNozzle[] nz = Acc.TF_nozzles(t);
                        if (nz != null) foreach (JetNozzle n in nz) ScaleAfterburners(n, Plugin.ThrustScale);
                    }
                }
                foreach (UnitPart up in a.partLookup)
                    if (up is AeroPart ap && dragScaled.Add(ap.GetInstanceID())) { Acc.AP_dragArea(ap) *= Plugin.DragScale; nDrag++; }
                // LERX effectiveness (area) and centre-of-mass shift (mass moved Fore -> Back, total unchanged)
                foreach (UnitPart up in a.partLookup)
                    if (up is AeroPart ap && up.gameObject.name.Contains("LERX") && Mathf.Abs(Plugin.LerxEffectiveness - 1f) > 1e-4f && areaScaled.Add(ap.GetInstanceID()))
                        Acc.AP_wingArea(ap) *= Plugin.LerxEffectiveness;
                if (Plugin.CgShiftAft > 1e-4f && !cgShifted.Contains(id))
                {
                    UnitPart fore = null, back = null; float mTot = 0f;
                    foreach (UnitPart up in a.partLookup)
                    {
                        if (up == null) continue;
                        mTot += up.mass;
                        if (up.gameObject.name.EndsWith("_Fore")) fore = up;
                        else if (up.gameObject.name.EndsWith("_Back")) back = up;
                    }
                    if (fore != null && back != null && mTot > 1000f)
                    {
                        Transform root = a.transform;
                        float dz = Vector3.Dot(fore.transform.position - back.transform.position, root.forward);
                        float m = Mathf.Min(Plugin.CgShiftAft * mTot / Mathf.Max(1f, dz), 0.6f * fore.mass);
                        fore.mass -= m; back.mass += m;
                        if (fore.rb != null && fore.rb != a.rb) fore.rb.mass = fore.mass;
                        if (back.rb != null && back.rb != a.rb) back.rb.mass = back.mass;
                        cgShifted.Add(id);
                        Plugin.Log.LogInfo($"F-22E CG shift: {m:F0} kg moved {fore.gameObject.name} -> {back.gameObject.name} ({dz:F2} m) = {m * dz / mTot:F3} m aft of {mTot:F0} kg");
                    }
                }
                foreach (ControlSurface cs in Acc.Collect<ControlSurface>(a))
                {
                    nSurf++;
                    if (servoScaled.Add(cs.GetInstanceID())) Acc.CS_servo(cs) *= Plugin.ActuatorScale;
                    AeroPart ap = Acc.CS_attached(cs) as AeroPart;
                    if (ap == null) continue;
                    string n = cs.gameObject.name;
                    float k = 1f;
                    if (Mathf.Abs(Acc.CS_pitchRange(cs)) > 0.01f || n.Contains("Elevator")) k = Plugin.StabilatorEffectiveness;
                    else if (n.Contains("Flap") || n.Contains("Aileron")) k = Plugin.FlaperonEffectiveness;
                    if (Mathf.Abs(k - 1f) > 1e-4f && areaScaled.Add(ap.GetInstanceID())) { Acc.AP_wingArea(ap) *= k; nArea++; }
                }
                bool complete = nSurf >= 6 && nFan >= 2;
                if (complete) { setupDone.Add(id); setupRetry.Remove(id); }
                Plugin.Log.LogInfo($"F-22E airframe setup on {a.name}: {nFan} engines (roll/yaw TVC removed, thrust x{Plugin.ThrustScale}), {nSurf} surfaces (servo x{Plugin.ActuatorScale}), {nArea} newly effectiveness-scaled, {nDrag} parts drag x{Plugin.DragScale}{(complete ? "" : " - incomplete, will retry")}.");
            }
            catch (Exception e) { Plugin.Log.LogError("F-22E airframe setup failed: " + e); }
        }

        public static FcsController GetOrCreate(Aircraft a)
        {
            int id = a.GetInstanceID();
            if (controllers.TryGetValue(id, out var c)) return c;
            if (failed.Contains(id)) return null;
            try
            {
                EnsureSetup(a);
                c = new FcsController(a);
                controllers[id] = c;
                foreach (var s in c.surfaces) { surfaceMap[s.cs.GetInstanceID()] = s; surfaceOwner[s.cs.GetInstanceID()] = c; }
                foreach (int did in c.flaps.ownedIds) deviceOwner[did] = c;
                if (c.inputs != null) byInputs[c.inputs] = c;
                return c;
            }
            catch (InvalidOperationException e)
            {
                // parts not all registered yet (spawn in progress) - stock controls this frame, retry shortly
                if (!attachRetry.TryGetValue(id, out int n)) n = 0;
                attachRetry[id] = ++n;
                if (n == 1 || n % 250 == 0) Plugin.Log.LogWarning("F-22E FCS waiting to attach (attempt " + n + "): " + e.Message);
                if (n > 1500) { failed.Add(id); Plugin.Log.LogError("F-22E FCS gave up attaching; stock flight controls stay active: " + e.Message); }
                return null;
            }
            catch (Exception e)
            {
                failed.Add(id);
                Plugin.Log.LogError("F-22E FCS could not attach; stock flight controls stay active for this aircraft: " + e);
                return null;
            }
        }

        public static void Fail(FcsController c, Exception e)
        {
            Plugin.Log.LogError("F-22E FCS fault, reverting this aircraft to stock flight controls: " + e);
            c.Failed = true; c.Engaged = false;
            if (c.ac != null) failed.Add(c.ac.GetInstanceID());
            Remove(c);
        }

        public static void Disengage(Aircraft a)
        {
            if (a == null) return;
            if (controllers.TryGetValue(a.GetInstanceID(), out var c)) c.Engaged = false;
        }

        public static void Remove(FcsController c)
        {
            c.Dispose();
            if (c.ac != null) controllers.Remove(c.ac.GetInstanceID());
            if (c.inputs != null) byInputs.Remove(c.inputs);
            foreach (int did in c.flaps.ownedIds) deviceOwner.Remove(did);
            // surfaces keep their mapping only until they are restored by the postfix
        }

        public static bool TrySurface(ControlSurface cs, out SurfaceEffector s, out FcsController c)
        {
            int id = cs.GetInstanceID();
            s = null; c = null;
            return surfaceMap.TryGetValue(id, out s) && surfaceOwner.TryGetValue(id, out c);
        }

        public static void ForgetSurface(ControlSurface cs)
        {
            int id = cs.GetInstanceID();
            surfaceMap.Remove(id); surfaceOwner.Remove(id);
        }

        public static bool TryInputs(ControlInputs ci, out FcsController c) => byInputs.TryGetValue(ci, out c);
    }

    internal static class Hooks
    {
        // ---------------- flight control law ----------------
        public static bool FilterPrefix(ControlsFilter __instance, ControlInputs inputs, Vector3 rawInputs, Rigidbody rb, float gForce, bool flightAssist)
        {
            Aircraft ac = Acc.CF_aircraft(__instance);
            if (!Plugin.Enabled || ac == null || !Registry.IsF22(ac)) return true;
            if (!ac.LocalSim || ac.Player == null) { Registry.Disengage(ac); return true; }
            FcsController c = Registry.GetOrCreate(ac);
            if (c == null || c.Failed) return true;
            try
            {
                c.Tick(inputs, flightAssist);
                return false; // stock FlyByWire skipped; pilot inputs stay raw (cockpit stick shows the real stick)
            }
            catch (Exception e)
            {
                Registry.Fail(c, e);
                return true;
            }
        }

        // ---------------- per-surface command injection ----------------
        public static void SurfacePostfix(ControlSurface __instance)
        {
            try
            {
                Aircraft a = Acc.CS_aircraft(__instance);
                if (a != null && Registry.IsF22(a)) Registry.EnsureSetup(a);
                if (!Acc.JobCreated(__instance)) return;
                ref ControlSurfaceFields f = ref Acc.Job(__instance);
                if (a != null && Registry.IsF22(a)) f.servoSpeed = Acc.CS_servo(__instance);

                if (!Registry.TrySurface(__instance, out SurfaceEffector s, out FcsController c)) return;
                bool drive = c.Engaged && !c.Failed && s.active && !Acc.CS_locked(__instance);
                if (drive)
                {
                    if (!s.overriding)
                    {
                        float tot = f.currentPitch + f.currentRoll + f.currentYaw;
                        f.currentPitch = tot; f.currentRoll = 0f; f.currentYaw = 0f;
                        s.overriding = true;
                    }
                    f.pitchRange = s.max; f.rollRange = 0f; f.yawRange = 0f;
                    f.controlInputs.pitch = Mathf.Clamp(s.cmdOut / s.max, -1f, 1f);
                    f.controlInputs.roll = 0f;
                    f.controlInputs.yaw = 0f;
                }
                else if (s.overriding)
                {
                    f.pitchRange = s.origPitchRange; f.rollRange = s.origRollRange; f.yawRange = s.origYawRange;
                    s.overriding = false;
                    if (c.Failed) Registry.ForgetSurface(__instance);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("F-22E surface hook: " + e.Message);
            }
        }

        // ---------------- flap devices (HighLiftDevice / AryxAlphaResponder) ----------------
        /// <summary>Skips the device's own schedule while the FCS flies the aircraft (FlapSystem drives it).</summary>
        public static bool FlapDevicePrefix(MonoBehaviour __instance)
        {
            try
            {
                if (__instance != null && Registry.TryDevice(__instance, out FcsController c) && c.Engaged && !c.Failed && c.flaps.Active)
                    return false;
            }
            catch { }
            return true;
        }

        // ---------------- TVC ----------------
        public static void TurbofanPrefix(Turbofan __instance)
        {
            Aircraft a = Acc.TF_aircraft(__instance);
            if (a != null && Registry.IsF22(a)) Registry.EnsureSetup(a);
        }

        /// <summary>Pitch input seen by the Turbofan's nozzle logic: the FCS nozzle command when engaged.</summary>
        public static float TvcPitch(ControlInputs ci)
        {
            if (ci != null && Registry.TryInputs(ci, out var c) && c.Engaged && !c.Failed) return c.tvcPitchInput;
            return ci != null ? ci.pitch : 0f;
        }

        public static float NozzleSlew(Turbofan t)
        {
            if (t == null) return 70f;
            Aircraft a = Acc.TF_aircraft(t);
            return a != null && Registry.IsF22(a) ? 70f * Plugin.ActuatorScale : 70f;
        }

        public static float NozzleSlewNeg(Turbofan t) => -NozzleSlew(t);

        public static IEnumerable<CodeInstruction> TurbofanTranspiler(IEnumerable<CodeInstruction> instructions)
        {
            FieldInfo fPitch = AccessTools.Field(typeof(ControlInputs), "pitch");
            MethodInfo mTvc = AccessTools.Method(typeof(Hooks), nameof(TvcPitch));
            MethodInfo mSlew = AccessTools.Method(typeof(Hooks), nameof(NozzleSlew));
            MethodInfo mSlewNeg = AccessTools.Method(typeof(Hooks), nameof(NozzleSlewNeg));
            int nPitch = 0, nSlew = 0;
            foreach (CodeInstruction ins in instructions)
            {
                if (ins.opcode == OpCodes.Ldfld && ins.operand is FieldInfo fi && fi == fPitch)
                {
                    var n = new CodeInstruction(OpCodes.Call, mTvc);
                    n.labels.AddRange(ins.labels); n.blocks.AddRange(ins.blocks);
                    nPitch++;
                    yield return n;
                    continue;
                }
                if (ins.opcode == OpCodes.Ldc_R4 && ins.operand is float fv && (fv == 70f || fv == -70f))
                {
                    var l = new CodeInstruction(OpCodes.Ldarg_0);
                    l.labels.AddRange(ins.labels); l.blocks.AddRange(ins.blocks);
                    yield return l;
                    yield return new CodeInstruction(OpCodes.Call, fv > 0f ? mSlew : mSlewNeg);
                    nSlew++;
                    continue;
                }
                yield return ins;
            }
            Plugin.Log.LogInfo($"Turbofan transpiler: {nPitch} pitch read(s) routed to FCS, {nSlew} nozzle slew constant(s) scaled.");
        }
    }

    internal sealed class Telemetry
    {
        private StreamWriter w;
        private readonly FcsController c;
        private float lastFlush;

        public Telemetry(FcsController c)
        {
            this.c = c;
            try
            {
                string dir = Paths.BepInExRootPath;
                string path = Path.Combine(dir, "F22E_FCS_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv");
                w = new StreamWriter(path, false);
                c.WriteHeader(w);
                Plugin.Log.LogInfo("F-22E FCS telemetry: " + path);
            }
            catch (Exception e) { Plugin.Log.LogError("Telemetry disabled: " + e.Message); w = null; }
        }

        public void Write(float t, float sp, float sr, float sy, float rho)
        {
            if (w == null) return;
            c.WriteRow(w, t, sp, sr, sy, rho);
            if (t - lastFlush > 1f) { w.Flush(); lastFlush = t; }
        }

        public void Close()
        {
            try { w?.Flush(); w?.Dispose(); } catch { }
            w = null;
        }
    }
}
