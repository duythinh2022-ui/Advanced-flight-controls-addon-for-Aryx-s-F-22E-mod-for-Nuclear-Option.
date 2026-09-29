# F-22E FCS 1.9.6 — AoA protection no longer overrides a nose-down stick at the limit

Base: 1.9.5. Log: 29 Sep 13:59 (four "stuck at max AoA" episodes).

## What the log showed

| t (s) | IAS m/s | AoA | Stick | Pitch-law ask | Commanded | Time to AoA < 40 |
|---|---|---|---|---|---|---|
| 47.6 | 50 | 65.6 | full push | −48 deg/s | +13 → +6 deg/s for 2.3 s | 3.5 s |
| 55.8 | 81 | 64.8 | full push | −48 | +10 | 0.9 s |
| 78.5 | 53 | 65.6 | full push | −48 | +12 | 1.8 s |
| 109.9 | 68 | 65.5 | full push, then released | −48 | +12.6 → +10.5, held until the stick came back | 8.9 s |

- In all four, `prot` = 0 (the flag only shows beyond limit + 2 since 1.9.3).
- Throughout, AoA sat at 65.5-65.6, just past limit + 0.5.

## Cause

- **The protection branch took the pitch command over.** Past limit + 0.5 deg, the AoA-protection branch set both the upper and the lower bound of the pitch-rate command to its own value, so the stick had no say.
- **What its value was.** Since 1.9.3 (continuous onset), that value is about the flight-path rate just past the threshold: +8 to +13 deg/s nose-up in these episodes.
- **What that did.** The jet was held at ~65.5 deg regardless of stick. Only when the correction grew, or the stick came back, did it come down.
- **Why it was not seen before 1.9.3.** Before 1.9.3 the branch subtracted an immediate ~14 deg/s, so the pinning showed up as a nose-down hitch instead.

## Fix

- **Upper side.** Protection is now only a bound in the nose-up direction. The command is min(protection, the pilot's own command within the normal bounds). A push, an ease or a release (zero-rate hold in rate command) now takes effect immediately at the limit.
- **Lower side.** Mirrored: max(protection, pilot).
- **Full aft stick.** Unchanged: the pilot's command there is already bounded by the capture to the flight-path rate, and protection's value is at or below it.

## Bench

- **The limit is hard to reach on the bench.** It holds 64.4-64.9 deg in max-AoA pulls (with or without roll/pedal), so it never reaches the 65.5 where protection took over.
- **Forced case (30 % throttle, cap off, 300 km/h).** AoA reached 65.5. Full push: 15 deg of AoA gone in 1.73 → 1.53 s.
- **In-game cases.** With the fix, the command in the logged states is the pilot's −48 deg/s instead of +10.
- **Regression suite.** Identical to 1.9.5.
- **Idle → AB check.** Identical to 1.9.5.

## Not verified in game

- The normal bench never reaches 65.5 deg, so the fix rests on the logic and the forced case.
- To check first in game: push, ease and release at the AoA limit (low IAS, full aft) should now act at once, with no hold near 65 deg.
