# F-22E FCS 1.9.7: AoA → pitch-rate hand-over on the negative side

Base: 1.9.6. Tom: "for negative aoa, gimme the same aoa-rate command blend".

## What changed

The 1.9.1 hand-over (with the 1.9.5 fixes) now runs mirrored on the push side. The code works in "side space" (s = +1 pull, −1 push: stick s·sp, AoA s·α, limit s·limit), so both sides run the same lines.

**Stick map (push)**
- **Scale.** Same as positive: −36° at −0.5 stick, 72° per full stick.
  - This is a feel change. In 1.9.6 the push side was linear to the limit, 45° per full stick, so −0.5 was −22.5°.
- **Threshold point.** Moves with the dynamic threshold, e.g. −25° → −0.347.
- **Full push = the negative AoA limit (−45°).** The jet goes through −36° at the rate budget.

**Rate command past the threshold**
- Crossing the threshold with the stick past its point switches to nose-down pitch-rate command. It starts from the rate at the crossing and runs to the budget at full push.
- Release or full push: 1:1 (0..full = 0..budget). Release holds zero rate.
- Easing below the point: stays rate command until the jet is back at the threshold, then blends into AoA command over 4°.
- Floating hold point: mirrored, same knee.
- Pulling while in negative rate command gives a nose-up rate (stick × budget) until the jet leaves the band, then positive AoA command.

**Threshold**
- 36° while the wings' negative lift is at least 2 g (`RateBlendLiftG`), falling to 25° at 1 g. Filtered at 0.5 s.
- New envelope points at −5..−45° give `nLiftMin`.
- The bench airframe's lift curve is nearly symmetric. At 300 km/h the peak is n 1.89 at +37.5° and −1.93 at −37.5°, so ±36 sits just below the stall on both sides.

**HUD.** Code 7 (RATE) now also appears in negative rate command. v[3] (the commanded rate) is negative there.

## New dev settings (2. Envelope)

- `HighAoARateBlendNegative` (true). false = 1.9.6 push side.
- `RateBlendAoANegative` (36).
- `RateBlendAoAMinNegative` (25).
- `RateBlendStick`, `RateBlendLiftG` and `RateBlendBand` are shared with the positive side.

## Bench (200 km/h level, afterburner unless noted)

| Push | 1.9.6 | 1.9.7 |
|---|---|---|
| −0.5 held | −22.5° hold | −36° (hand-over engages at −36.1 after 5 s, then drifts to −37 by 7 s: rate hold) |
| −0.7 held | −31.5° hold | crosses −36 at 27°/s → rate command holds 27°/s → captured at the −45 limit |
| −0.7, eased to −0.45 before crossing | n/a | stays AoA command, settles −32.4° |
| −0.7 → −0.3 in rate command | n/a | 11.7°/s until back at −36, then AoA command to −21.6° |
| Full push, release | limit −45, release holds zero rate | same; rate command from −36.7° |
| Full push, then +0.5 | n/a | nose-up 24°/s out of rate command, then positive AoA command to 36° |
| Full push, then full pull | −40 in 0.44 s | 0.48 s |
| Idle, full push, then full pull | −40 in 2.82 s | 2.82 s (the idle nose-up authority limit, see 1.9.6) |

- **Where it can be reached.** Above ~250 km/h the −3.5 g limit stops a push well before −36°: at 300 km/h a full push peaks at −30°. So the negative hand-over only matters at low speed, e.g. push-overs at the top of a climb.
- **Positive side unchanged.**
  - Regression suite identical to 1.9.6, with the negative blend on and off.
  - Idle → afterburner full-pull runs bit-identical to 1.9.5/1.9.6.
  - With the negative blend off, the push side is the 1.9.6 map.
- **Floating hold not exercised on the push side.** At the speeds where −36 is reachable, the rate at the crossing is above the flight-path rate, so the jet never came back to the threshold with the stick past its point. The code is the positive-side path, run mirrored.
- **Edge case (same as the positive side).** When the dynamic threshold is a hair under 36 (lift ratio just below 2 g), holding exactly 0.5 is just past the threshold point. It then hands over to rate command at the crossing and drifts about 1° over 2 s.

## Telemetry

New columns: `rateBlendAoANeg`, `nLiftMin`, `hiSide` (+1 / −1 while in rate command, 0 otherwise).

## Not verified in game

All bench-only, including the HUD showing RATE with a negative rate.
