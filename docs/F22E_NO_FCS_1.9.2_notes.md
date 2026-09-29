# F-22E FCS 1.9.2 — yaw braking floor re-referenced to 270 km/h, smooth low-IAS margin schedule

Base: 1.9.1. Only the high-AoA yaw braking floor changed.

## Change

**Yaw braking floor** (`HighAoABrakeFloor`, applies from 25 to 35 deg AoA).
- **Stop time.** The yaw-rate limit is at most stop time × yaw authority × qPredK. The stop time is:
  - T = YawStopFactor × low-IAS yaw boost at 270 km/h / margin(IAS).
  - That is 1.08 s (1.8.45-1.9.1: 280 km/h, 1.03 s).
- **margin(IAS).** Worked out with the IAS in km/h:
  - 1 above 210 km/h (plateau);
  - smoothstep up to 1.5 at 160 km/h;
  - 1.5 below 160 (plateau).
  - The smoothstep has zero slope at both ends, so the curve has no corner at 210 or 160.
- **Soft join with the normal yaw law.** The floor now joins the normal yaw limit with a C1 soft minimum. Width is 12 % of the pair's mean, and the result is at most 3 % below the smaller limit where they cross. So there is no corner where the floor takes over, near 270 km/h.
- **Roll floor.** Unchanged (RollStopFactor 1.0 s), apart from the same soft join.

**New dev keys (3. Lateral-directional):**

| Key | Default |
|---|---|
| `YawBrakeRefIAS` | 270 |
| `YawBrakeMarginStartIAS` | 210 |
| `YawBrakeMarginFullIAS` | 160 |
| `YawBrakeMarginLowIAS` | 1.5 |

## Bench, 65 deg AoA, full aft stick with alternating full pedal (falling leaf), median over IAS bins

| IAS km/h | 1.9.1 rMax (deg/s) / stop time (s) | 1.9.2 rMax / stop time | margin |
|---|---|---|---|
| 300 | 50.7 / 0.84 | 52.1 / 0.87 | 1.00 |
| 270 | 43.4 / 0.89 | 45.5 / 0.93 | 1.00 |
| 240 | 37.3 / 0.98 | 39.2 / 1.02 | 1.00 |
| 210 | 28.9 / 1.03 | 30.6 / 1.07 | 1.00 |
| 195 | 25.4 / 1.03 | 25.3 / 1.04 | 1.04 |
| 180 | 22.2 / 1.03 | 19.0 / 0.89 | 1.22 |
| 165 | 18.5 / 1.03 | 13.6 / 0.75 | 1.44 |
| 150 | 15.6 / 1.03 | 11.0 / 0.72 | 1.50 |
| 120 | 11.9 / 1.03 | 8.0 / 0.73 | 1.50 |

- **Stop time.** The stop time here is rMax / yaw authority, including the IAS-decay factor, which was ≤ 1 in these runs.
- **Falling leaf from 250 km/h** (IAS 216 → 130): mean |r| 12.5 → 9.0 deg/s, |β| max 11.6 → 16.5 deg. At the 8-11 deg/s the limit allows below 160 km/h, the pedal builds sideslip instead of yaw rate.
- **Regression suite.** Unchanged except S5 (post-stall roll+yaw at 65 deg): yaw rate at 200 km/h 22.1 → 19.6 deg/s, at 300 km/h 32.6 → 33.4.

## Falling-leaf surface wiggle: not reproduced, not fixed

- **Bench comparison.** On the bench, 1.9.1's falling leaf (200-350 km/h entries, alternating pedal) has about the same surface activity as with the floor off (1.8.44 behaviour):
  - stab/flap reversals 1.4-3 per second, ±4.5-5.5 deg about a 1 s mean, in both.
- **What the bench does show.** When the yaw rate reaches the limit while IAS is falling, the limit keeps coming down (up to ~10 deg/s²). The rate trajectory then turns from accelerating to braking in one step, and the surfaces swing from full pro-yaw to full anti-yaw.
- **Tried and reverted.**
  - A moving-target trajectory, which rides the falling limit. On the bench: sideslip 12 → 20 deg and IAS 136 → 115 km/h.
  - A 0.35 s low-pass on the floor. No measurable effect, and it lags the anticipation.
- **Still needed.** A telemetry log (`Telemetry = true`, dev menu `0. Debug`) of the falling leaf where it wiggles. Columns to look at: `r`, `rCmd`, `rMax`, `aYaw`, `qPredK`, `iasDot` and the surface columns.
