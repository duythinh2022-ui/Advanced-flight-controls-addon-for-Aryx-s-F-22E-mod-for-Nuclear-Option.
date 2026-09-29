# F-22E FCS 1.9.4 — 20 % less yaw stop time at 290-330 km/h

Base: 1.9.3. One change, and only at high AoA (the braking-floor gate, 25-35 deg and up).

## Change

- **What it does.** Between 290 and 330 km/h IAS, the high-AoA yaw-rate limit is multiplied by 0.8. That is 20 % less yaw braking margin in Tom's terms: less allowed stop time, so less yaw rate for the same authority and faster braking.
- **Transitions.** Smoothstep ramps outside the band: 260 → 290 km/h and 330 → 360 km/h.
- **What it applies to.** The combined limit. At these speeds the normal yaw law, not the floor, is usually the lower of the two, so scaling only the floor would have done nothing.
- **Roll.** The roll limit is unchanged.
- **Dev keys (3. Lateral-directional):** `YawStopTimeMidFactor` 0.8, `YawStopTimeMidLoIAS` 290, `YawStopTimeMidHiIAS` 330, `YawStopTimeMidRamp` 30.

## Bench, full aft + full lateral stick at 65 deg, yaw-rate limit (median per IAS bin)

| IAS km/h | 1.9.3 | 1.9.4 | ratio |
|---|---|---|---|
| ≥ 360 | — | — | 1.00 |
| 345 | 51.5 | 50.0 | 0.97 |
| 330 | 51.2 | 41.5 | 0.81 |
| 300 | 48.6 | 38.9 | 0.80 |
| 285 | 46.5 | 37.7 | 0.81 |
| 270 | 43.8 | 38.6 | 0.88 |
| ≤ 255 | — | — | 0.99-1.00 |

## Regression suite

Identical to 1.9.3 except:
- S6 high-AoA release + roll, 350 km/h: peak roll rate 21 → 30 deg/s, qMin −0.8 → −1.2.
- S4 at 450 km/h: pMax 20 → 21.

Not verified in game.
