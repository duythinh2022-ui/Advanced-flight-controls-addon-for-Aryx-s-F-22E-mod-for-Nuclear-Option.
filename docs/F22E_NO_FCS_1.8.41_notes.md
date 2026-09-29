# F-22E Strike Raptor FCS 1.8.41 (Nuclear Option)

1.8.40 plus two lateral changes. Both DLLs get them. The nozzle rework is unchanged.

## What the 2026-09-28 logs show

**Pedal at low AoA (20:08 log, 98–102 s, 330–380 km/h, 7–10° AoA, no roll stick)**
- The stabs and flaperons are **not** wired to the pedals. Pedal commands sideslip (`PedalSideslip`, 15° at full pedal below 36° AoA). That becomes a yaw-rate command, and the allocator spreads the yaw-acceleration demand over every surface by its modelled moments.
- The horizontal surfaces move for two reasons:
  1. **Cancelling the canted rudders' roll.** On the bench, 12° of rudder makes ~28 °/s² of yaw and ~62 °/s² of roll. That is legitimate, and it stays.
  2. **Buying yaw once the rudders run out.** This is the "crazy" part. The rudders sat at 30–35°, and the allocator took the rest of the yaw from the flaperons' drag difference. It rolled them one way and the stabs the other, so the roll cancelled out. In the log: stabs L +35 / R −17, flaperons at ±25, reversing with each pedal reversal. Roll still wandered to 24 °/s against a 4 °/s command.
- **Vertical tails shot off:** reason 2 is then the only yaw source, so all of the yaw demand goes into that split.

**Rolling at the restricted AoA with a store (11:09 log)**
- **28° cap** (0.08 m lateral CG offset), full aft + full roll at AB: the nozzles were on the −20° stop for most of 209–220 s. Roll rate was ~68 °/s, held down by the coupling limiter. AoA held within ~0.5°.
- **What makes the pitch-up there:** the down-going stab is stalled (local AoA 33–40°). The stabs can only roll by one stab going trailing-edge up, which is nose-up. The flaperons add to it, and the nozzles carry all of it.
- **12° cap** (0.24 m): the roll–pitch priority pass mostly kept the nozzles near trim, and the stabs took the pitch.
  - The inertial coupling at 175 °/s was ~130 °/s² there.
  - The nozzles still went to −15…−20° in the left rolls (231–232 s) and on reversals.
  - Releasing the stick during a roll stop at 247.3 s overshot the cap by 1.3°. The nozzle band opened too late.

## 1. Roll-fight cost while the pedals are in (`4. Gains / RollFightCost`, 5)
- **What it does:** it adds a cost to the allocator while the pedals are in, and for 1 s after.
  - The surfaces are in three groups: stabilators, outboard flaperons and inboard flaperons.
  - A group whose roll opposes the net roll of the horizontal surfaces costs `RollFightCost` × its roll acceleration squared.
  - Groups rolling the same way cost nothing.
- **Without pedal, nothing changes:** rolls allocate exactly as in 1.8.40. I tried an always-on version first. It made the pull–release–roll nose-down dip worse (S1 mean qMin −3.5 → −5.2 °/s), so it is pedal-gated.
- **Tried and dropped:**
  - Hiding the horizontals' yaw from the allocator below ~35° AoA. It roughly tripled the sideslip in full-rate rolls (β 3.6° → 10° at 8° AoA). At 26–32° AoA it also cost 35–65 % of the roll, because there the flaperons and stabs' yaw is what coordinates the velocity-vector roll.
  - A pedal-sideslip governor on rudder travel. On the bench, the split also happens with the rudders short of their stops, so it did nothing.

**Bench, full pedal for 1.5 s, trim throttle, 1.8.40 → 1.8.41**

| Case | Opposing group roll, mean (max) °/s² | Stab differential | Outboard / inboard flaperon differential | Roll error rms | β |
|---|---|---|---|---|---|
| 340 km/h, 8° AoA | 24.3 (88) → 3.8 (40) | 5.7 → 4.7° | 14.1 / 14.1 → 4.2 / 4.7° | 2.7 → 1.7 °/s | 12.3° both |
| 450 km/h | 10.7 (35) → 0.0 (0.2) | 2.3 → 1.7° | 6.6 / 6.1 → 4.2 / 2.9° | 1.7 → 1.6 °/s | 12.5° both |
| 340 km/h, both fins + rudders detached | 79 (132) → 8.9 (40) | 14.5 → 1.4° | 27 / 31 → 8 / 13° | 2.0 → 4.2 °/s | 6.1 → 5.4° |

- **What it costs:**
  - **No tails:** the pedals barely yaw the jet any more (yaw-rate error rms 11.8 → 12.3 °/s). Roll wanders a bit more (peak 4 → 10 °/s).
  - **Pedal sideslip** is unchanged: full pedal still asks for 15°, and the rudders still hit their stops at 340 km/h.
- `RollFightCost = 0` gives 1.8.40.

## 2. Pitch-reserve roll governor (`4. Gains / RollPitchReserve`, 0.15)
- **What it measures:** while a roll is commanded, it watches how much of the nozzles' nose-down travel is left at the live thrust (nose-up at negative AoA). This is measured state, with no AoA or speed schedule.
- **What it does:** when the margin falls below the reserve, it lowers the roll-rate limit and the roll-entry acceleration. It gives them back as the margin returns.
- **Floors:** 35 % of the rate limit (never below 30 °/s) and 50 % of the entry acceleration.
- **Where it acts:** below ~18° AoA the roll–pitch priority pass already keeps the nozzles near trim, so it does nothing there.

**Bench, full roll at a held AoA, 1.8.40 → 1.8.41**

| Case | Nozzles on the −20° stop | Bank at 1 / 2 s | Max AoA over command | q error rms |
|---|---|---|---|---|
| Clean, 26° AoA, AB, 450 km/h | 85 % → 26 % | 30/110 → 29/87° | 0.6 → 0.3° | 1.7 → 2.1 °/s |
| 800 kg store (22° cap), AB, 490 km/h | 35 % → 18 % | 29/87 → 28/77° | 0.6 → 0.4° | 1.4 → 2.6 °/s |
| Roll during a pull to 26° at 550 km/h, AB | 44 % → 28 % | 44/118 → 42/88° | 0.7 → 0.3° | 3.1 → 3.0 °/s |
| 2.5 t store (12° cap), 560 km/h, part throttle | 0 % both | 58/169° both | 0.2° both | 0.95 °/s both |

- **What it costs:**
  - 10–25 % of the roll angle at 2 s in held rolls between ~20 and 35° AoA.
  - The pitch-rate error rms rises a little (it is the roll rate settling onto the limit). AoA overshoot did not get worse in any case.
- **Other reserves I ran:**
  - 0.25: nozzles on the stop 15 % / 3 % in the first two cases, bank at 2 s 82 / 77°.
  - 0.10: 33 % / 19 %, 88 / 83°.
- `RollPitchReserve = 0` gives 1.8.40.

## Standard suite
S1 pull–release–roll, S2, S3 1 g rolls, S5 post-stall roll + pedal, S6 and S7 are identical to 1.8.40. S4 held pull 0.4 (26° AoA) + roll: bank at 1 s 27/42/54 → 27/41/50° at 350/450/550 km/h.

## Telemetry
Two new columns after `rpBand`:
- `rollGov`: roll-limit factor, 1 = no effect.
- `rollGovMargin`: fraction of nozzle nose-down travel left.

## Not checked
- **Nothing in game yet:** all numbers are from the bench (Aryx's airframe, your current config values). The bench store is point mass added to the left rear wing, not your real loadout.
- **Release during a roll stop** (the 247.3 s overshoot at the 12° cap) is not addressed. The governor only acts on the roll limit, and that case is a band-timing issue in the roll–pitch priority pass.

## Source
The bench is back in the source zip under `bench/`, updated to build against the 1.8.41 sources and to read your config values: `genplugin2.py` regenerates the Plugin stub from `Plugin.cs` plus your cfg. It has two new scenario options, `storeKg` and `storePart`, and the test scripts `fight.sh`, `hold.sh` and `rehold.py`.
