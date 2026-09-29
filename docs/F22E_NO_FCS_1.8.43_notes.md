# F-22E FCS 1.8.43 — one DLL, performance profile in the config

Base: 1.8.42. The flight control is unchanged: a bench re-run of the 300 km/h reversal case matched 1.8.42 exactly.

## Change

- **One DLL.** The Buffed and StockPerf builds are merged into one `Aryx_F22E_StrikeRaptor_FCS.dll`. The `STOCK_PERF` compile switch is gone.
- **New key.** `1. General / PerformanceProfile`: `Stock` (default) or `Buffed`. It shows as a dropdown in the ConfigurationManager (F1).
  - **Stock:** Aryx's original thrust, parasitic drag, fuel burn, flares, EW capacity/recharge and gun ammo. There is no AoA or transonic drag relief. The nine performance keys are ignored but keep their values.
  - **Buffed:** those nine keys are used as set. Their defaults are the old Buffed build's values: thrust x1.08, drag x0.885, AoA/transonic relief 0.25/0.2, fuel x0.75, flares x10, ammo x2, EW x1.5/x1.2.
  - **Same in both:** FCS, surface ranges, TVC, LERX, CG shift, nozzle rig.
- **When a switch takes effect.** Change the profile before spawning:
  - thrust, parasitic drag, countermeasures and ammo are set when a jet spawns and stay until it respawns;
  - fuel burn follows the setting live;
  - drag relief can be turned off live but only turned on at spawn.
- **Config file.** One config file, `Aryx_F22E_StrikeRaptor.FCS.cfg`. The old `...FCS.StockPerf.cfg` is no longer read.
- **Version string.** Now defined once (`FcsStandalonePlugin.Version`). The "ready" log line now shows the version and the active profile; before this it still said 1.8.40.

## Tom's install

- `PerformanceProfile = Buffed` was added to the existing `BepInEx/config/Aryx_F22E_StrikeRaptor.FCS.cfg`. Every other value is unchanged.
- 1.8.42 is backed up as `Aryx_F22E_StrikeRaptor_FCS.dll.1.8.42.bak`.

Changed source: Plugin.cs, FcsStandalonePlugin.cs. The bench's genplugin2.py stubs `StockPerformance` as false.
