# F-22E Strike Raptor FCS 1.7.6 (Nuclear Option)

1.7.6 fixes the nose-down shove in tail slides with the AoA-protection override held. Every other bench result is
unchanged from 1.7.5 (differences ≤0.3 °/s and ≤0.4° AoA, all in segments past 120° AoA; no NaN in the 60 s
stress test), in both builds.

## Cause
I reproduced it with a new bench test (T42): vertical climb at idle to 80 % throttle, stick neutral until the jet
stops, then full aft stick (override latched, zero pitch rate commanded) while it slides back through 90–180° AoA.

| Idle, full aft from the stop | 1.7.5 |
|---|---|
| AoA 172° → 146° | nose-down 1.5 → 26 °/s and growing, to **−53 … −84 °/s** |
| Stabs | parked at 0°, modelled pitch authority **0** |
| TVC | 0°, idle thrust: no moment to give |

The law was commanding zero pitch rate; the allocator had nothing it thought it could use:
- **Stabs.** Their pitch model is forced monotone in the normal direction (+deflection = nose up), so a stalled stab
  is never used "backwards". In a tail slide, the air reaches the
  stabs over the trailing edge (local AoA 150–175°). A stab still makes plenty of pitch moment there, but in the
  opposite deflection sense. The raw curve at 38–47 m/s of backward speed: −30° deflection = +12…+16 kNm, 0° =
  −5…−10 kNm per stab. The monotone rule flattened that to zero, so the stabs sat still.
- **Rudders** are faded out above 50° AoA, so nothing damped yaw either. In long tail-first falls (15–30 s, 100–125
  m/s) the jet eventually swapped ends in yaw. It then dropped into the 75–95° AoA band, where there really is no
  authority at idle.

## Fix
1. **Reversed-flow stab model:** when a stab's own local AoA is past 120° (hysteresis: back below 110°), its
   monotone pitch model follows the reversed direction, so nose-up = trailing edge down. Between 60° and 110°
   (deep stall either way) the normal-direction clamp stays, as before.
2. **Rudders come back in reversed flow:** the 35→50° AoA fade-out is undone from 120° to 140° AoA. The fins are in
   clean reversed flow there, and the allocator's full (unclamped) rudder model already carries the reversed sign.

## Results (T42, buffed; stock performance matches within 1 °/s)
Full aft held until the end of the run. "Pull at stop" = full aft from a standstill at the top. "Pull at 25 m/s" =
full aft while still climbing, which also covers the long tail-first fall after it.

| Throttle | Pull at | 1.7.5 pitch rate while held | **1.7.6** | tail-first speed reached |
|---|---|---|---|---|
| idle | stop | −53.3 … +42.5 (11 s nose-down) | **−0.2 … +0.2** | 164 m/s |
| idle | 25 m/s | −10.7 … +44.2 (4.9 s) | **−0.3 … +3.8** | 156 m/s |
| 20 % | stop | −84.1 … +48.3 (3.3 s) | **−0.9 … +0.1** | 143 m/s |
| 40 % | stop | −0.4 … +52.6 (tumbled through 180°) | **0.0 … 0.0** | 112 m/s |
| 60 % | stop | −0.4 … +5.0 | −0.2 … +5.0 | 61 m/s |

Nose-down time (>5 °/s) is **0.0 s in all 18 held cases**, in both builds.

**Letting go mid-slide** (release 4 or 10 s after the pull, 17 cases): the protection brings the nose round and the
jet is nose-first (|AoA| < 30°) after 3.5–7.2 s. Lateral stays quiet: |p|, |r| ≤ 5 °/s and |β| ≤ 0.5° after the
recovery. At idle the nose-down rate during that recovery peaks at −52…−70 °/s, above the 48 °/s command,
because the aero moment is doing most of the work. It is in the recovery direction, so I left it.

## Limits
- The bench reproduces the shove near 165–175° AoA. You saw it at 130–140°. Both are the same mechanism (reversed
  flow on the stabs, zero authority). The exact AoA depends on sink speed and CG, and the bench aero past 90° is the
  game's airfoil table, not wind-tunnel data. If it still shoves in game, a telemetry CSV will show whether
  `stabPitchFrac` stays at 0 through the slide.
- Past ~120° AoA with the override held you are riding a tail-first fall on purpose. The FCS now holds the attitude,
  so the fall can build to 150+ m/s at idle. Ease the stick to let the protection fly it out.

No new config keys.
