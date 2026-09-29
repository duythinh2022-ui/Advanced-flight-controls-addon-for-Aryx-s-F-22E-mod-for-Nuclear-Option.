# F-22E FCS 1.8.45 — asymmetric cap: smoothing, +~3°, IAS-decay anticipation; high-AoA lateral braking floor

Base: 1.8.44. Logs: 29 Sep 00:02 / 00:12 / 00:20. The load in all three was 2 Scimitar outer + 3 Scythe inner per side, with some dropped, giving a 6.5–6.8 cm lateral CG offset.

## What the logs showed

- **Twitch at full aft stick (00:20, t 195–330 s, 36–40° AoA, 80–105 m/s).**
  - The raw cap (`asymCap`) is recomputed on every 0.2 s sweep (4.4 per second in this log) and jumped 1.3° median, 4.1° p90 and 7.8° p99 each time.
  - The effective cap followed it at the 10°/s slew, which gave ±1.5–2° swings at about 1 Hz.
  - `qT` swung 7 → 19 °/s with it.
  - This is noise from the demand curve being flat: the demand rises only ~0.025 per degree near the cap, so ±0.05 of noise in the demand moves the cap ±2°.
- **Conservatism.** The same flat curve means 3° past the cap raises the demand only to ~1.08.
  - At 90 m/s the reserve is bound at 80 % of the roll authority, so the load may use only ~20 % of it (after the 5 °/s² deadband).
  - Measured at the cap: roll authority 108 °/s², load 21.5 °/s², so 86 °/s² is left on the weak side.
- **Low IAS (≤45 m/s).** The cap released to 65°, because the load there (≈4 °/s²) falls under the fixed 5 °/s² deadband.
  - Kept as it was. Scaling the deadband with q held the cap at ~20° at 150–200 km/h on the bench; that change was not asked for, so I left it out.

## Changes

1. **Cap filter.**
   - Median of the last three sweeps, then an asymmetric low-pass: rises with τ 1 s (`AsymCapSmoothing`), drops with τ 0.3 s. Then the existing tighten/relax slew.
   - Replaying the filter on the 00:20 raw cap:

     | | Old | New |
     |---|---|---|
     | Effective-cap high-pass rms | 0.84° | 0.24° |
     | \|rate\| p90 | 11.8 °/s | 3.8 °/s |
     | Mean | — | 0.6° lower |

   - Bench, 600 kg store (6 cm), full pull at 330 / 450 / 600 km/h, α command high-pass rms: 0.75 / 0.63 / 0.37 → 0.26 / 0.26 / 0.19.
2. **Cap demand 1.0 → 1.08** (`AsymCapDemand`).
   - In-game slope ≈ 0.025/deg, so ~+3° with this load. Heavier loads gain less: bench 1500 kg +0.9°.
   - **Bench says more for the 600 kg case: +3 to +7°** (the bench's demand curve is flatter than the game's). The in-game number is the calibration.
3. **IAS-decay anticipation.**
   - IAS rate is filtered (τ 0.25 s).
   - Deceleration is weighted by w² where w = (d − 6)/(20 − 6), clamped to 0–1 (`DecelStart`/`DecelFull`). Ordinary bleed (≤10 m/s²) predicts almost nothing.
   - IAS is projected 1 s ahead (`DecelLookahead`), and k = (IAS_pred/IAS)², with a minimum of 0.35.
   - The sweep judges the demand at k·q: load and authority scale by k, the deadband and absolute reserves do not.
   - The high-AoA rate limits (item 4) use the same k.
   - Bench full-pull entry from 450–600 km/h: k minimum 0.55–0.65.
4. **High-AoA braking floor** (`HighAoABrakeFloor`), faded in from 25° to 35° AoA.
   - Roll-rate limit ≤ RollStopFactor (1.0 s) × min(go, stop) roll authority × k.
   - Yaw-rate limit ≤ YawStopFactor × yaw boost at 280 km/h (= 1.03 s) × yaw authority × k.
   - The stop times are those of the reference manoeuvre (full pedal + full lateral stick, 65° AoA, 280 km/h). In game at 80 m/s the stop times were roll 30/35 = 0.86 s and yaw 50/53 = 0.94 s.
   - Replaces there the fixed 30 °/s roll / 20×boost yaw floors. At 40 m/s those floors gave stop times of 2.3–2.8 s (roll 30 on 11 °/s² authority, yaw 34 on 15).

## Bench, full pull + full roll corkscrew (+ pedal), roll released after 3–4 s, 1.8.44 → 1.8.45

| Case | Stop after release | Angle to stop | Body r max |
|---|---|---|---|
| 450 km/h clean | not stopped in 1 s (>50°) → 1.16 s | 23.8° | 65 → 54 |
| 450 km/h clean + pedal | not stopped in 1 s → 1.18 s | 24.5° | 64 → 52 |
| 350 km/h clean | 1.80 → 1.20 s | 35.8 → 21.0° | 56 → 46 |
| 350 km/h clean + pedal | 1.84 → 1.28 s | 35.5 → 21.7° | 53 → 45 |
| 450 km/h, 600 kg store | 1.88 → 1.40 s | 37.9 → 27.1° | 38 → 35 |

**Store corkscrew:** |β| max 4.6 → 8.1° at entry. The cap is ~6° higher on the bench (item 2), so the entry reaches 45° AoA with the roll.

## Regression suite (vs 1.8.42-44)

- **S1–S4, S6, S7:** within noise.
- **S5, post-stall roll+yaw at 65°:** yaw rate at 200 km/h 34 → 23 °/s, at 300 km/h 37 → 32 °/s; β 3.5 → 2.5. This is the braking floor biting below the reference IAS: less yaw rate in slow post-stall yaw (falling leaf).
- **Held-roll set:** clean cases identical. Store cases:
  - 2.5 t at 600 km/h: nozzle min −12.3 → −7.6.
  - 800 kg at the 22° cap: overshoot 1.5 → 0.2.
  - 1.8 t AB reversal: overshoot 2.8 → 3.6°, bank @1 s 66 → 62, nozzle mean −8.4 → −10.5.

## Telemetry

New columns: `iasDot`, `qPredK`, `asymCapRaw`, `asymCapFilt`.

## Not verified in game

All of it. The filter replay uses the logged raw cap; the new raw cap will differ (threshold 1.08, anticipation).
