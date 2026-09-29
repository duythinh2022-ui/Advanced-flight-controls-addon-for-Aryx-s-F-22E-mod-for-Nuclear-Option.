# F-22E FCS 1.8.42 — roll pitch-fight cost, nozzle seam shading

Base: 1.8.41. Two changes: an allocator cost against the stabs pitching the nose up while the nozzles pitch it down during rolls, and a separate dark material for the nozzle seam bands. Everything else is the same as 1.8.41.

## What the logs showed (21:33 asymmetric, 21:36 clean)

- **Roll reversal with the store (21:33, t ≈ 71.8 s).** Stab L was pinned at −30° (local AoA ≈ 40°, deep stall) and R at −17° (33°). Both stabs were trailing edge up, and the roll came from a stalled differential. The stop was late (the hitch). When the reversal band let the nozzles go, they swung nose-down and the jet dropped from α 12.8° to 6.9°.
- **Clean, low q (21:36, ~55 m/s), after a roll stop.** The R stab sat at +35° with local AoA −33° (deep negative stall) while the other stab was near neutral: the pattern you saw. The rudders were at ±30°. The flaperons were split asymmetrically with no pedal input; this is a post-roll yaw-braking split. The nozzles were at −8…−10°.
- **Clean, 0–5° AoA while rolling.** The stabs were collectively TE-up +10…+35°, with the nozzles at −5°. The stabs made roll plus pitch-up, and the nozzles cancelled it. This is the "TVC pitch-down at low AoA" you reported.
- **Mechanism near the stabs' roll reversal.** The TE-down stab is at or past its stall, so it adds little roll. The allocator keeps getting roll from the TE-up stab, and that stab's lift is nose-up. The linear pitch model had no cost for that pitch-up, because the nozzles cancel it for free in pitch-acceleration terms. The nozzles' nose-down travel is what paid for it.

## Change 1: roll pitch-fight cost (`4. Gains / RollPitchFightCost`, default 1, 0 = 1.8.41)

- **Groups.** The allocator treats the stabs and the TVC as two pitch groups. When the stab group pitches the nose up (nose-down at negative AoA) and the nozzle group pitches it down, the group opposing the net pitch is charged K·(its pitch acceleration)², in rad/s².
- **Which pattern counts.** Only stabs nose-up with nozzles nose-down. The opposite pattern (stabs nose-down with nozzles nose-up) is the roll-pitch-priority trim, and it is not charged. Penalising both patterns made S1 worse (qMin mean −3.4 → −5.2).
- **Where it applies.** The cost is wired into the Gauss-Newton step, the grid pass and the stab+TVC paired search.
- **Gate.** The cost is active while a roll is commanded (the rpLat hold). It is off:
  - while the pitch-rate command is moving (|q̇cmd| > 25°/s², 0.6 s hold);
  - in AoA protection;
  - on the pitch-priority run-away (`pitchNeed`);
  - while the nose is coming up faster than commanded (q − qT > 3…8°/s).
- **Reversal band.** Tried and not shipped: dropping the reversal nozzle let-go (`RpRevBand` 12 → 0 with a pitch tolerance). It halved the nozzle swing on reversals but raised the AoA overshoot (600 km/h reversal 3.9 → 6.0°; 1.8 t store 3.5 → 4.7°). `RpRevBand` stays 12.

### Bench, held pull + full roll (+ reversal), roll window + 1.5 s, 1.8.41 → 1.8.42

| case | result | verdict |
|---|---|---|
| 200 km/h, 16° AoA, roll stop | nozzle min −13.6 → −10.3, mean −5.0 → −2.0, over 1.1 → 0.3° | better |
| 300 km/h, 19° AoA, reversal | nozzle min −20 → −12.9, mean −9.8 → −5.7, over 1.2 → 0.5° | better |
| 600 km/h, 2.5 t store, reversal | nozzle min −20 → −12.3, over 2.9 → 0.9°, q err +11 → +4.7 | better |
| 450 km/h AB, 1.8 t store, reversal | over 3.5 → 2.8°; nozzle min still −20 | better |
| 26° AoA (pull 0.4) | stab past 35° local AoA 32 % → 0 %, q err −8 → −2.4 | better |
| 22° cap, 800 kg store | stab past 35° 35 % → 3 %, but bank @1 s 28 → 20° | mixed |
| 19° AoA (pull 0.3) | bank @1 s 42 → 36°, else ≈ | worse |
| **600 km/h, 13° AoA, reversal** | **over 3.9 → 8.3°**, nozzle min −11.9 → −13.3 | **worse** |
| **300 km/h, 10° AoA, reversal** | **over 1.1 → 2.7°** | **worse** |
| 8° AoA | unchanged | — |

- **Why the 600 km/h reversal overshoots.** On the reversal the allocator avoids the stab-up / nozzle-down pattern by driving the other stab TE-down past its stall (local −18°). That stab gives less nose-down than modelled, so α undershoots to 9.7° (1.8.41: 11.0°). The outer loop then commands q ≈ 48 to recover, and α overshoots. The q-error fade cut this from 9.0° to 8.3° but did not fix it. It is the same family as the in-game +35° / −33° stab park, and the stall model is the root cause.
- **Regression suite.** S1, S2, S3, S5, S6 and S7 are within noise. S4 (26° AoA held + full roll): |β|max 1.3 → 3.7 at 350 km/h and 2.1 → 3.5 at 450 km/h; bank @1 s 27 → 25 at 350 km/h.

## Change 2: nozzle seam shading (`6. Visuals / NozzleSeamShade`, default 0.07)

- The 136 seam-band triangles in each flap were re-assigned from Aryx's skin (slot 0) to a new fifth material slot. These are the gaps between the root, the floating cowl and the end flap.
- The new slot is a plain URP/Lit clone at grey 0.07, like the cavity parts. For reference, the black liners are 0.035 and the dark-grey beams 0.085.
- An atlas UV change was not used, because the atlas grey under the seams is 0.22 on Skin_B and varies by livery.
- The value is applied when a jet spawns. **Not checked in game.**

## Known / not fixed

- **In-game stab park past stall.** The −30° (local 40°) and +35° (local −33°) parks are not reproduced on the bench, which parks at about 30–35° local.
- **Post-roll flaperon yaw split without pedal at low q.** `RollFightCost` is pedal-gated by design (`FightMode` 0), so it does not act here.
- **Reversal nozzle let-go (`RpRevBand`).** Still the trade-off between the nozzles' nose-down swing and AoA overshoot.
- **In-game A/B.** Set `RollPitchFightCost` = 0 to get 1.8.41 behaviour.

## Files

- `F22E-StrikeRaptor-FCS-1.8.42-Buffed.zip`
- `F22E-StrikeRaptor-FCS-1.8.42-StockPerf.zip`
- `F22E_FCS_1.8.42_src.zip` (includes the bench)

Changed source: Allocator.cs, Effector.cs, FcsController.cs, Plugin.cs, NozzleRig.cs, NozzleMeshData.cs, FcsStandalonePlugin.cs.
