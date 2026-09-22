## F-22E Strike Raptor FCS 1.7.6

This is a flight-control overhaul for **Aryx's F-22E Strike Raptor 1.0.4** (Nuclear Option, BepInEx 5). It is a
companion plugin: keep the Aryx mod installed and add **one** of the two builds below.

| Download | Build | Thrust / drag | Flares | EW storage / recharge | 20 mm ammo | Fuel burn |
|---|---|---|---|---|---|---|
| `F22E-StrikeRaptor-FCS-1.7.6-Buffed.zip` | Buffed | ×1.08 / ×0.885 | ×10 | ×1.5 / ×1.2 | ×2 | ×0.75 |
| `F22E-StrikeRaptor-FCS-1.7.6-StockPerf.zip` | Stock performance | original | original | original | original | original |

Both builds share the same flight control laws and aero/control tweaks: pitch-only TVC, flaperon ×1.5, stabilator
×1.1, LERX ×1.6, CG 5 cm aft and servos ×1.3. Unzip into the game folder so the DLL lands in `BepInEx/plugins/`.
When switching builds, delete the other FCS DLL first.

### Changes in 1.7.6
- **Tail slides:** no more nose-down shove with the override held. The stabs and rudders now work in reversed flow
  (past ~120° AoA), so the override holds the attitude all the way down. Ease the stick and the protection flies the
  jet out nose-first.

### Changes in 1.7.5
- **AoA-protection override:** after the recovery, the jet no longer stays frozen at zero pitch rate while you keep
  pulling. The override hands back to the normal law once AoA is 5° inside the limit. It now arms at 85 % stick.
- **Takeoff rotation:** with the mains on the ground and the nose wheel up, the stick commands pitch rate (full =
  8 °/s) and neutral holds the nose attitude.
- **Buffed only:** 20 mm ammunition ×2 (the extra rounds add their weight) and fuel consumption −25 % at every
  throttle setting.

### Since 1.6.5
Two builds, stronger stabs in low-IAS rolls with the TVC kept near trim, hitch-free roll reversals, a curved low-IAS
yaw margin, full yaw feed-forward at moderate and high AoA, and the AoA-180° wrap fix.

Full notes and bench data are in `docs/` (1.7.0 to 1.7.6). The tuning was done on a 6-DOF bench running the game's
aero model. Please report in-game behaviour, ideally with a telemetry CSV (`Telemetry = true` in the config).

SHA-256
```
73f9d332367c509ea8f51f7e1cb479bc41e45579296d94ae6941caecc7a740a4  Aryx_F22E_StrikeRaptor_FCS.dll
a5fb0bddeec9dabf3eabf11be2a6e1269f1f361135ef9387e7dd2dc534e1d28e  Aryx_F22E_StrikeRaptor_FCS_StockPerf.dll
```
