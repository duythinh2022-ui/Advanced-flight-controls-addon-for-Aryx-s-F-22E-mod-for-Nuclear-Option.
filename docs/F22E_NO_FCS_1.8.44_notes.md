# F-22E FCS 1.8.44 — player config trimmed, dev/debug unlock DLL

Base: 1.8.43. The flight control is unchanged: a bench re-run of the 300 km/h reversal case matched 1.8.42/43 exactly.

## Player config (`Aryx_F22E_StrikeRaptor.FCS.cfg`, main DLL)

| Section | Keys |
|---|---|
| 1. General | `PerformanceProfile` (Stock default / Buffed), `Enabled`, `StickSmoothing` |
| 2. AI | `AIFlightControl`, `AIDirectRoll`, `AIGunRollFade`, `NozzleAnimationAI` |
| 3. Countermeasures - Stock profile | `FlareCountScale` 1, `GunAmmoScale` 1, `EWCapacityScale` 1, `EWRechargeScale` 1 |
| 4. Countermeasures - Buffed profile | `FlareCountScale` 10, `GunAmmoScale` 2, `EWCapacityScale` 1.5, `EWRechargeScale` 1.2 |

- **Countermeasures.** Each profile has its own set of four values, and each value is set independently. The active profile picks which set applies. They take effect at spawn.
- **Buffed performance.** The Buffed thrust/drag/fuel/relief values (x1.08, x0.885, 0.25/0.2, x0.75) are no longer in the player config. They use their built-in values; the dev menu can change them.
- **Removed from the player config:**
  - `Telemetry`, `AIFlightLog` and `GroundAeroBrakeLog`. All three are off unless the dev DLL is in and turns them on. `AIFlightLog` now defaults to off.
  - `ConfigVersion`, together with the default-migration code behind it.
  - The dead `Ground/VentDoorsFollowAirbrake`, which has not been read since 1.8.37.

## Dev/debug unlock (`Aryx_F22E_StrikeRaptor_FCS_Dev.dll`, optional)

- **What it is.** A separate, empty BepInEx plugin (GUID `Aryx_F22E_StrikeRaptor.FCS.Dev`). The main plugin soft-depends on it, so the dev plugin loads first and the main plugin finds it.
- **With the dev DLL.** Every other setting binds to `BepInEx/config/Aryx_F22E_StrikeRaptor.FCS.Dev.cfg` and shows in F1 under "F-22E Strike Raptor FCS - Dev/Debug settings". That covers the envelope, lateral, gains, airframe, visuals, asymmetric load and ground sections, plus `0. Debug` with Telemetry, AIFlightLog and GroundAeroBrakeLog.
- **Without the dev DLL.** Those settings sit at their built-in defaults in memory and nothing is written. The dev file is left alone and read again when the DLL comes back.
- **No migration of dev defaults.** With `ConfigVersion` gone, an existing dev file keeps whatever values it holds when a later build changes a default.

## Upgrade migration (one-time, automatic)

- **Player file.** Keys left over from older builds become orphans. Keys that only moved section (the AI toggles, and the old Countermeasures values into the Buffed set) are carried over by key name.
- **Dev file.** If the dev DLL is present and the dev file doesn't exist yet, the dev settings are carried over from the old player file.
- **Cleanup.** The orphans are then dropped from the player file.
- **Checked** with a BepInEx 5.4.23 ConfigFile harness on Tom's current cfg:
  - player file: only the 15 keys above, with Buffed and his countermeasure values;
  - dev file: `SideslipGuardStart = 990` carried over.
  - Not yet run in game.

## Tom's install

- Both DLLs are in plugins. 1.8.43 is backed up as `Aryx_F22E_StrikeRaptor_FCS.dll.1.8.43.bak`.
- The config was left as it was; the migration runs on first launch.
- Without the dev DLL, `SideslipGuardStart` goes back to its default of 4 (Tom's 990 lives in the dev file).

Changed source: Plugin.cs, FcsStandalonePlugin.cs, new DevUnlock/FcsDevPlugin.cs. build.sh excludes DevUnlock/. The dev DLL is built on its own against BepInEx + UnityEngine.
