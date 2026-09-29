# F-22E FCS 1.9.3 — more yaw rate at 150-210 km/h, roll-ceiling limit cycle removed, AoA-protection hitch removed

Base: 1.9.2. Log: 29 Sep 13:10 (max-AoA turns on lateral stick, 150-300 km/h).

## 1. Low-IAS stop time: the other direction (1.9.2 had it backwards)

- **What changed.** From 210 to 150 km/h the allowed stop time now grows smoothly (smoothstep) to 1.5× the 270 km/h reference, and stays flat below 150.
  - Result: 50 % more yaw rate for the same authority, with slower braking, as asked.
  - 1.9.2 shrank the stop time instead, to 1/1.5.
- **Roll too.** The roll stop time gets the same factor. At 65 deg AoA a velocity-vector turn needs body roll = cos(AoA) × the turn rate. With the roll left at 1.0 s, the roll limit capped the turn, and with it the yaw rate, at ~160 km/h.
- **Config.** The dev keys are now `YawStopTimeStartIAS` 210, `YawStopTimeFullIAS` 150 and `YawStopTimeLowIAS` 1.5. 1.9.2's `YawBrakeMargin*` keys are no longer read.

## 2. Yaw rate jumping high 20s ↔ mid 30s: a limit cycle through the roll ceiling

**From the log** (t 319-323 s, 72 m/s, 65 deg, full lateral stick):
- The roll floor used the stop-side roll authority, `aRollNeg` = authority − measured trim (`rollTrim`, from dTrimR).
- That measured trim is the turn's own roll moment (dihedral × sideslip, yaw-rate roll). It swung 20 ↔ 29 deg/s² at 1.4 Hz, in phase with r and β.
- With it:
  - roll ceiling 9 ↔ 18 deg/s;
  - velocity-vector ceiling 20 ↔ 42 deg/s;
  - yaw command 22 ↔ 37;
  - r 27 ↔ 40;
  - stabs and flaperons swinging full travel.

**Fix.**
- The stop-side authority is now the full roll authority minus only the modelled asymmetric-load trim (beyond the deadband), low-passed 0.5 s.
- The turn's dynamic roll moment is left out, because it falls away as the turn is braked.
- Store trim still counts.

## 3. Pitch hitch at max AoA: AoA protection onset was a step

**From the log** (t 315.3 and 325.8 s):
- AoA reached 65.5 (limit + 0.5). The protection branch then took sqrt(2·aUp·e) + 4 ≈ 14 deg/s off the pitch-rate command in one frame (qCmd 25 → 10).
- The nozzles swung −13 → +20, and AoA dropped 3-4.6 deg before recovering.

**Fix.**
- The correction starts at 0 at the threshold and grows linearly at 8 deg/s per deg until it meets the braking curve.
- The +4 now ramps in over 2 deg.
- The protection state (pitch priority in the allocator, fight costs off) is set only beyond limit + 2 deg.
- Otherwise the jet sat in that band and toggled it.

## Bench

**Full aft + full lateral stick, held (max-AoA turn), 1.9.2 → 1.9.3.**

| Entry | Yaw rate (mean, 5-95 %) | AoA min | Protection active |
|---|---|---|---|
| 280 km/h | 18.0 (11-35) → 26.8 (22-35) | 63.5 → 64.4 | 2 % → 0 |
| 330 km/h | 21.9 (13-39) → 27.8 (22-39) | 63.6 → 64.4 | 4 % → 0 |

- **280 km/h entry:** yaw rate holds 26-28 deg/s from 210 down to 158 km/h. In 1.9.2 it decayed 30 → 11 deg/s over the same span.
- **Velocity-vector ceiling oscillation** (1 s high-pass): 0.38 → 0.18.

**Falling leaf, alternating full pedal, from 200 km/h:**
- mean |r| 9.0 → 16.5 deg/s;
- |β| max 16.5 → 5.8 deg;
- rudder activity 28 → 8 deg/s.

**Costs (braking).** Corkscrew stop after full-stick release, 1.9.2 → 1.9.3:

| Case | Stop time | Angle over |
|---|---|---|
| 450 km/h | 1.16 → 1.54 s | 24 → 32 deg |
| 350 km/h | 1.26 → 1.56 s | 24 → 31 deg |
| 600 kg store | 1.28 → 1.34 s | — |

1.8.44 took 1.8 s at 350 km/h. The cost is mostly from item 2: the measured turn trim no longer shortens the roll limit.

**Regression suite:** S5 post-stall roll+yaw at 200 km/h: yaw rate 19.6 → 27.2 deg/s, β 2.2 → 3.0. Everything else identical, including S1/S2, so the protection change did not alter pull-release recoveries.

## Not verified in game
