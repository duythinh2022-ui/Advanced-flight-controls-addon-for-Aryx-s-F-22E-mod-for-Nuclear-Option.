# F-22E FCS 1.9.5: low-thrust AoA cap comes back at once on throttle-up; floating hold no longer latches full stick

Base: 1.9.4. Tom's report: at idle the jet caps AoA, but after throttling up, even to max AB, the limit came back only slowly. He had to release and re-pull to get the full AoA.

## Cause (two bugs, both 1.9.1)

1. **Cap rise limit.** 1.9.1 limited how fast the cap may rise: 4 deg/s, and 1 deg/s while the jet sits at or above the cap. This was to stop the part-throttle pitch limit cycle.
   - With full stick held at the idle cap (~26 deg), the jet is at the cap, so the cap came back at 1 deg/s.
   - Bench: 350 km/h, full stick, idle → max AB. The cap was only at 36.9 deg after 11 s.
2. **Floating hold latched at full stick.** At low IAS the cap recovery pushed the nose down through the rate-command threshold while the stick was full aft.
   - The floating hold point then latched stick 1.0 as "hold the threshold".
   - From then on, full stick commanded the threshold (25-31 deg) until the stick was released.

## Fix

1. **Fast rise on thrust gain.** Each sweep compares the nozzles' nose-down authority with the previous sweep.
   - When it has grown by more than 1 deg/s² (throttle up, spool-up), the cap may rise as fast as it drops (τ 0.3 s) for the next 1 s. The window refreshes while thrust keeps rising.
   - A rise that comes from the airframe side (IAS changes) keeps the 1.9.1 limits. The limit cycle stays fixed.
2. **Floating hold latching.** It now latches only on a pilot ease:
   - stick < 0.95, and
   - not while an envelope cap is pushing the nose down.
   - A full pull (> 0.98) always cancels it.
3. **`HighAoARateBlend = false`.** Full stick is the AoA limit again. In 1.9.1-1.9.4 the stick map stopped at the threshold (36 deg) even with the blend off.

## Bench: full stick held, idle → max AB at t = 3 s

| IAS | 1.9.4 | 1.9.5 |
|---|---|---|
| 250 km/h | AoA stuck ≤ 31.7 (floating hold at full stick) | cap ≥ 60 in 0.7 s, AoA 60 in 2.2 s |
| 350 km/h | cap 36.9 after 11 s | cap ≥ 60 in 0.7 s, AoA 60 in 1.7 s |
| 450 km/h | same as 350 | cap ≥ 60 in 0.7 s, AoA 60 in 1.7 s |

- **Regression suite:** identical to 1.9.4.
- **Part-throttle full pull + ease (the 1.9.1 limit-cycle case):** identical to 1.9.4.
- **Low-thrust full pull + push:** unchanged (caps 23.4 / 26.0 / 33.4, all recover).

## Still open

- Overshoot of the cap in a yank at part throttle. Bench, 400 km/h, 50 % throttle: AoA 49.8 against a 38 cap, then recovers.

## Not verified in game

The bench steps thrust instantly. In game, the fast window depends on the spool-up adding more than 1 deg/s² of nozzle authority per sweep (~0.2-0.4 s). AB adds ~100 deg/s² over the spool, so this should hold, but it has not been checked.
