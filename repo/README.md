# F-22E Strike Raptor FCS (Nuclear Option)

This is a flight-control-system overhaul for **Aryx's F-22E Strike Raptor** mod for *Nuclear Option*. It is a companion
BepInEx plugin: it runs next to the unmodified Aryx mod DLL. On the player's F-22E it replaces the stock
fly-by-wire with a full FCS, and it applies a small aerodynamic/control re-balance to every F-22E.

> Requires **Aryx's F-22E Strike Raptor 1.0.4**. This plugin contains no aircraft, models or assets. Without the
> Aryx mod it does nothing.

## Which build?
Both builds share every control law and every aero/control tweak. They differ only in performance and
countermeasure defaults.

| | **Buffed** | **Stock performance** |
|---|---|---|
| File | `Aryx_F22E_StrikeRaptor_FCS.dll` | `Aryx_F22E_StrikeRaptor_FCS_StockPerf.dll` |
| Engine thrust | ×1.08 | original |
| Parasitic drag | ×0.885 (about −10 % straight-line drag) | original |
| Flares | ×10 | original |
| EW (jammer) storage / recharge | ×1.5 / ×1.2 | original (not touched) |
| 20 mm gun ammunition | ×2 | original |
| Fuel consumption | ×0.75 (−25 %, every throttle setting) | original |
| Config file | `BepInEx/config/Aryx_F22E_StrikeRaptor.FCS.cfg` | `BepInEx/config/Aryx_F22E_StrikeRaptor.FCS.StockPerf.cfg` |

Install **one** of them. Both use the same plugin GUID, so if both are present BepInEx loads only one and logs a
warning.

## Install
1. Install BepInEx 5 (tested with 5.4.23) for Nuclear Option.
2. Install Aryx's F-22E Strike Raptor 1.0.4. `Aryx_F22E_StrikeRaptor_1.0.4.dll` goes in `BepInEx/plugins/`.
3. Copy the FCS DLL of the build you want into `BepInEx/plugins/`. If you are switching builds, remove the other
   FCS DLL first.
4. Start the game once. The config file is created on first launch. The log (`BepInEx/LogOutput.log`) should show
   `F-22E FCS 1.7.6 ready`, and, when you spawn an F-22E, `F-22E FCS attached: 8 surfaces …`.

## What it does
**Airframe (every F-22E, AI included, applied at spawn)**
- Pitch-only thrust vectoring (±10°). The stock roll/yaw TVC is removed.
- Flaperon/aileron effectiveness ×1.5, stabilator ×1.1, LERX ×1.6, and the CG moved 0.05 m aft. The result is
  relaxed stability: the neutral point is 0.09 m aft of the CG (stock 0.20 m), with less trim deflection and less
  trim drag.
- All control-surface and nozzle servos ×1.3 faster.

**Flight control laws (the player's F-22E)**

| Condition | Pitch law | Limits |
|---|---|---|
| Weight on wheels, nose wheel down | direct (like stock) | — |
| Takeoff/landing rotation (mains down, nose wheel up) | pitch-rate command, full stick 8 °/s; neutral holds the nose attitude | — |
| Gear down | pitch-rate command | AoA +65/−45°, +9.5/−3.5 g |
| Gear up, Stability Assist ON | blended AoA / G command (AoA at low speed, G at high speed); neutral stick = zero pitch-rate hold | AoA +65/−45°, +9.5/−3.5 g |
| Stability Assist OFF (MPO) | pitch-rate command | no AoA limit, +12.5/−3.5 g |

- **AoA protection:** beyond the AoA limit the nose is driven back at up to 48 °/s. Opposing stick scales that
  recovery down, and full stick against it (≥ 90 %) holds zero pitch rate (override). The override stays latched until you
  ease the stick or AoA is 5° back inside the limit. After that the normal law resumes, so aft stick pulls again. In a tail slide (past ~120° AoA) the stabs
  and rudders work in reversed flow, so the override holds the attitude there too.
- **Roll:** velocity-vector (stability-axis) roll with sideslip regulation, on a jerk-limited trajectory. The rate
  ceiling is the original aircraft's rate +20 % (low IAS) / +10 % (high IAS), capped at 250 °/s. Stops are shaped
  to end when the command does.
- **Yaw:** body yaw rate ≤ 75 °/s. Above 15–25° AoA it is also capped at 0.7 × the measured yaw authority, so the
  roll can be stopped again. That margin widens by up to ×1.35 at very low IAS (falling leaf).
- **High AoA:** above 36° AoA the pedals blend into the velocity-vector roll. The rudders fade out from 35° to 50° AoA.
- **Control allocation:** an INDI inner loop plus a nonlinear, stall-aware allocator over every surface and the
  nozzles. The nozzles carry the pitch trim (in low-AoA rolls the stabs take it over, so the nozzles stay near trim),
  and each surface's own local AoA is modelled, so a stalled surface is not asked for more.
- Flaps and outer flaperons are scheduled by the FCS: a high-lift schedule, stall-capped, with load relief at high g.
- Stick smoothing (60 ms) and actuator damping (acceleration-limited servo commands, full servo speed kept).

All of the above is tunable in the config file. Every key has a description. Defaults whose meaning changes
between versions are migrated once through `ConfigVersion`.

## Telemetry
Set `Telemetry = true` to write a 50 Hz CSV (`BepInEx/F22E_FCS_<time>.csv`) with the FCS state: rates, commands,
AoA/β, limits, allocator saturation, flap schedule and more. This is the most useful thing to attach to a bug
report.

## Other mods
- `FcsApi.GetPitchCommand(Aircraft, float[])` is a small public API that other plugins can find by reflection. It
  returns the active pitch law, commanded AoA/G, protection state and limits. A flight-data readout can use it to
  show the FCS command.
- In multiplayer, only single-player code paths were checked. The host's values may override the client's
  flare/EW numbers.

## Known limits
- The laws were developed on a 6-DOF bench that runs the game's aero equations on the F-22E part layout. The bench
  has no transonic effects, so behaviour above ~450 m/s is less verified.
- At idle thrust and very low airspeed the nose can sag at high AoA even with zero pitch rate commanded, because
  there is not enough pitch authority.
- The yaw axis at 35–50° AoA is authority-limited (rudders fading, TVC pitch-only). Roll stops there take ~1.5 s.

## Building from source
You need the .NET SDK (6 or later) and a Nuclear Option install with BepInEx 5.

```
dotnet build -c Release -p:GameDir="C:\Program Files (x86)\Steam\steamapps\common\Nuclear Option"
dotnet build -c Release -p:Variant=StockPerf -p:GameDir="C:\Program Files (x86)\Steam\steamapps\common\Nuclear Option"
```

The build compiles against the game's `NuclearOption_Data/Managed` assemblies and `BepInEx/core`. If yours are
elsewhere, override `-p:ManagedDir=…` and `-p:BepInExCoreDir=…`. No NuGet packages are needed.

## History
See [CHANGELOG.md](CHANGELOG.md). The per-version development notes, with bench data for every change, are in
[docs/](docs/).

## Credits
- **Aryx**: the F-22E Strike Raptor aircraft mod this plugin builds on. All aircraft content is Aryx's.
- FCS overhaul: Tom.
