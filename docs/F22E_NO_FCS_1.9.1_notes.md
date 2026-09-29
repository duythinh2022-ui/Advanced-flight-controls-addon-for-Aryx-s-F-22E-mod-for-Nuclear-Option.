# F-22E FCS 1.9.1: stick map for the AoA → pitch-rate hand-over, HUD rate label, low-thrust cap rise limit

Base: 1.9.0. Only the AoA-command side of the pitch law and the low-thrust cap's rise have changed. The G regime, the command shaping and the hybrid INDI are as in 1.9.0.

## Stick map (Tom's spec, option A: fixed AoA per stick)

**Fixed scale**
- `RateBlendAoA` (36) sits at `RateBlendStick` (0.5), so the scale is 72 deg per full stick (1.9.0: 65).
- The threshold's stick point moves with the dynamic threshold: 36 deg → 0.5, 24 deg → 0.333.

**Before the jet reaches the threshold**
- Up to the threshold point: AoA command on the 72 deg/stick line. Holding 0.5 holds 36 deg.
- Past the point: still AoA command, from the threshold up to the envelope limit at full stick.
  - Full stick is the same as commanding 65 deg: the jet goes through 36 at the rate budget, with no braking at the threshold.
  - Easing back to or below the point before the crossing keeps it in AoA command.

**Crossing the threshold with the stick past the point**
- The stick becomes a pitch-rate command. It starts from the pitch rate at the crossing, with no jump.
  - Pulling further runs from that rate up to the budget at full stick.
  - Easing runs linearly from that rate down to zero.
- A full pull (stick > 0.98) or a release switches it to the straight line 0..full stick = 0..budget. Released stick holds zero rate.
- Easing below the threshold point stays rate command until the jet is back at the threshold. Rate and AoA law are then blended over `RateBlendBand` (4 deg) below it.

**Floating hold point**
- If the jet comes back down to the threshold while the stick is still past the threshold point, that stick position becomes the threshold.
- Easing from there commands the live threshold with a 2 deg soft knee. It keeps doing so until the stick meets the threshold's own point on the 72 deg/stick line, and is caught there with no jump.
- If the dynamic threshold rises while you ease, its point moves up to meet the stick sooner.
- Pulling back past the held position is AoA command beyond the threshold again, which leads back into rate command.

**Envelope limit (asymmetric-load or low-thrust cap)**
- Limit at or below the threshold: the whole stick maps onto the limit, as before.
- Limit within 4 deg above the threshold: the two maps are blended.

**Bench, 300 km/h, afterburner**

| Stick | Result |
|---|---|
| Full | 48 deg/s straight through 36 deg, rate command from 39.8 deg; release holds zero rate |
| 0.5 held | 36.0 deg hold, same capture as 1.8.45 |
| 0.7 | AoA command 47.6 → crosses 36 at 32 deg/s → rate command holds 32 deg/s |
| 0.7, eased to 0.45 before the crossing | stays AoA command, settles 32.4 deg |

## HUD

- `FcsApi` code 7 (API version 2) means high-AoA pitch-rate command, with v[3] = the commanded rate.
- `NOFlightDataReadout.dll` is patched in its IL with Mono.Cecil (no source): code 7 now shows `RATE n°/s`. The other labels are unchanged.
- The patch has not been run in game.

## Low-thrust cap: rise rate limited (a fix for a 1.9.0 regression)

- **Problem.** In 1.9.0 the cap rose exponentially after a drop, at up to ~10 deg/s. The capture then re-accelerated the nose into the still-rising cap, and the recovery pushed it back.
- **What it caused.** Bench, 350 km/h, 50 % throttle, full pull then 0.62 stick: a ~1 Hz pitch-rate cycle of 2-18 deg/s at 55-60 deg AoA.
- **Fix.** The cap now rises at most 4 deg/s, and 1 deg/s while the jet is within 1 deg of it or above it. It still comes down with a 0.3 s time constant.

**Still open.** In a full-aft yank at part throttle and higher IAS, the cap comes down during the pull and the jet overshoots it.
- Bench, 400 km/h, 50 % throttle: cap 38 deg, AoA peaks at 49.8, recovers to 38, then settles at 41. At 70 % the peak is 52 against a cap of 48.
- Cause: the cap depends on the flaperons' symmetric trim, which moves with AoA (flap schedule). At low AoA the sweep does not know the trim the flaperons will have at high AoA.
- It recovers every time; it does not depart.
- Candidate fix: evaluate the flaperons at their scheduled trim for each AoA in the sweep.

## Regression suite (vs 1.9.0)

- Stick 0.4 is now 28.8 deg (72 deg/stick) instead of 26 (65 deg/stick). That is expected from the new scale and shows in S4 (AoA at roll 26 → 28.5-29.4, bank at 1 s 1-4 deg lower).
- S2 qMin worst −0.7 → −2.2 deg/s. S1 qMin worst −6.8 → −7.7.
- Everything else within noise.
- Low-thrust full pull then full push: zero thrust, ~0.1 T/W and 30 % all still recover (caps 23.4 / 25.9 / 33.4).

## Telemetry

New columns: `floatOn`, `floatS`.
