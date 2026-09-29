# F-22E FCS 1.9.0: pitch command shaping, hybrid INDI, AoA-to-rate hand-over, low-thrust AoA cap

Base: 1.8.45. All five changes are dev-config toggles. With all of them off, the bench suite matches 1.8.45 exactly.

## 1. Hybrid INDI pitch loop (`HybridIndi`, default 1): cause of the G overshoot/undershoot cycle

**Root cause.** The incremental pitch loop estimates the airframe's own pitch moment from filtered sensors (0.04 s). At high dynamic pressure that moment moves fast with AoA and pitch rate: the fuselage/LERX nose-up grows with AoA. The filter lag therefore let the pitch rate run above its command, by about 2 deg/s for 0.3 s at 1300 km/h. At that speed 1 deg/s of pitch rate is about 0.7 g.

**Fix.**
- The lag is predicted from the airframe model: dM/dAoA × (AoA − filtered AoA) + dM/dq × (q − filtered q).
- The slopes come from the non-control-surface parts and are re-evaluated every 0.1 s.
- The term fades out from 25 to 40 deg AoA. It only matters at high dynamic pressure, and at high AoA it cost roll onset.
- `ReleasedMomentLead` is scaled by (1 − HybridIndi). It was compensating the same lag for released stick, and both together gave a slow pitch-rate tail after release.

## 2. Command shaping (`CommandShaping` + `Shaping*`)

**What it does.**
- A pre-filter shapes the AoA and G commands. Its speed grows with the size of the step:
  - AoA: 1.5 1/s for a tiny step up to 10 1/s at 15 deg and above (quadratic in between).
  - G: 1.5 to 10 1/s over 3 g.
- A small step also ramps its onset over 0.25 s.
- The step size is held, decaying at one span per second, so a large step keeps its speed all the way in.
- With the stick released, the reference sits on the jet's present AoA/G.
- The idea matches the self-adjusting pre-filter gain of the T-50 control law (60 % gain below 1 g of error, 100 % above 3 g).

**Other changes in this area.**
- The capture limit uses the raw stick AoA, not the shaped one.
- The G undershoot capture now acts only while the load is being taken off. Its 0.15 g dead zone was pinning a steady hold 0.15-0.2 g high.
- With the stick released in the G regime, the pitch rate is taken off over ~0.3 s instead of in one step. The zero-rate hold itself is unchanged.

**AoA steps, 300/450 km/h (1.8.45 → 1.9.0).**

| Step | Onset pitch accel (deg/s²) | Time to 50 % (s) |
|---|---|---|
| 3 deg | 62 → 30 / 57 → 14 | 0.50 → 0.80 |
| 5 deg | 84 → 37 / 82 → 43 | 0.50 → 0.72 |
| 10 deg | 124 → 79 / 127 → 99 | 0.50 → 0.60 |
| 15 and 20 deg | unchanged | unchanged |

- Holding 26 deg with stick noise: AoA std 0.096 → 0.021 deg, max error 0.27 → 0.12.

**G at 1000 m, held 4 s then released (1.8.45 → 1.9.0).**

| Case | Peak / then | Onset (g/s) | Release (g/s) |
|---|---|---|---|
| 1300 km/h, 2 g | 2.81 / 1.78 → 2.06 / 2.06 | 3.4 → 1.1 | 1.5 → 1.2 |
| 1300 km/h, 3 g | 4.49 / 2.46 → 3.25 / 3.11 | 6.8 → 2.9 | 3.0 → 2.0 |
| 1300 km/h, 5 g | 7.20 / 4.17 → 5.30 / 4.95 | 12.4 → 7.7 | 6.2 → 3.8 |
| 1100 km/h, 3 g | 4.04 / 2.85 → 3.27 / 3.12 | | |

- Time to 90 % for a 1 g step is now about 1.4 s (was ~0.5 s). This is deliberate; `ShapingGGainSmall` sets it.
- A 3 g hold still sits about 0.1 g high after 4 s, from the slow integrator.

## 3. AoA-to-pitch-rate hand-over (`HighAoARateBlend`, `RateBlendAoA` 36, `RateBlendAoAMin` 25, `RateBlendLiftG` 2, `RateBlendBand` 4)

**Threshold.**
- The hand-over AoA is 36 deg while the wings alone can lift at least 2× the weight: q̄·S·CLmax/(m·g), thrust not counted.
- As that ratio falls to 1 g, the threshold drops linearly to 25 deg. On the bench that is about 330 km/h (full 36) and about 210 km/h (25).
- The threshold is smoothed with a 0.5 s filter.

**Stick mapping.**
- The stick keeps its AoA scale (AoA limit per full travel) up to the threshold. Below the threshold, nothing changes from 1.8.45.
- The travel beyond the threshold is the rate zone: from the rate that holds the threshold AoA (the flight-path rate), up to the pitch-rate budget.
- The capture brakes the jet onto the threshold arriving at that rate, so the pitch rate is continuous through the threshold.

**Above the threshold: pitch-rate command.**
- The map runs through (0, 0), the entry point (stick, rate at the crossing) and (full stick, budget).
- A release drops the entry point. Released stick holds zero rate, and the next pull maps 0..full stick straight onto 0..budget.
- Push commands a nose-down rate of stick × budget.
- The hand-over disengages below threshold − 4 deg. Across that band the output is blended with the AoA law.

**Unchanged.**
- The envelope limit (65 or the cap) still captures kinematically, and the protection is unchanged.
- The G regime is unaffected (the hand-over needs betaG < 0.5). AI, MPO and gear-down are unaffected.

**Bench, 300 km/h, stick 0.8.**
- 1.8.45: AoA command 52 deg. The capture took the pitch rate from 48 to 12 deg/s between 32 and 50 deg.
- 1.9.0: 45-48 deg/s straight through 36 deg, then the capture into 64 deg. Release holds zero rate; a new pull to 0.5 gives 24 deg/s (0.5 × 48).

## 4. Low-thrust nose-down-authority AoA cap (`LowThrustAoACap`, `LowThrustCapMargin` 8 deg/s², `LowThrustCapFloor` 20)

**How it works.**
- Every asymmetric-load sweep (0.2 s), the cap is set to the AoA where the nose-down authority runs out, minus 1 deg.
- The authority counted: stabilators full nose-down, flaperons at their symmetric trim, and the nozzles at the present thrust.
- The flaperon trim is followed only while no roll or yaw is commanded. Otherwise one flaperon hitting its stop in a roll shifted the pair's mean and dropped the cap mid-roll.
- Above the thrust-vectoring cut-off (Turbofan `thrustVectoringMaxAirspeed` 450 m/s against `Unit.speed`, the true/ground speed) the nozzles count as zero.
- The cap tightens with a 0.3 s time constant and relaxes with 1 s. It joins the asymmetric cap: the stick is remapped onto it, and the existing recovery handles being above it.
- It uses a new sweep array. `asymNetDn` leaves the flaperons' lift out, which made it ~10 deg pessimistic. `asymNetDn` itself is unchanged, so the asymmetric-load cap does not change.

**Bench no-return AoA** (hold, then full push), used to set the margin:

| Thrust | 300 km/h | 500 km/h |
|---|---|---|
| Zero | 24 ok, 28 departs | 28 ok, 32 departs |
| ~0.1 T/W | 32 ok, 36 departs | 32 ok, 36 departs |

**Cap at 350 km/h:** zero thrust 23.4, ~0.1 T/W 25.9, 30 % throttle 33.6, afterburner none.

**Full pull then full push (1.8.45 → 1.9.0).**
- Zero thrust: 80 deg and no recovery → 24.6 deg, recovers.
- 0.1 T/W: 73 → 26.8, recovers.
- 30 %: 63 → 36, recovers.
- Recoveries at 200-300 km/h all worked with margin 8. Margins 4 and 2 departed at 250 km/h.

## 5. Why the jet pitched up at high Mach, low IAS, with frozen nozzles

- NO turns thrust vectoring off above 450 m/s true speed (Turbofan.FixedUpdate zeroes `nozzleAngles`). At 16 km that is about 600-650 km/h IAS.
- Without the nozzles, the stabs cannot hold much above ~20-25 deg AoA, so the AoA ran away until the jet slowed below the cut-off.
- Bench, 16 km, 800 kg per wing:

| IAS, stick | 1.8.45 | 1.9.0 |
|---|---|---|
| 600 km/h, 0.6 | 74 deg | 14 deg |
| 600 km/h, 1.0 | 75 deg | 23 deg, cap 24 |
| 650 km/h, 1.0 | 88 deg | 22 deg |

- Not covered: why it also happens, less often, at low altitude in dense air. The cut-off needs 1620 km/h there. A telemetry log of that case would settle it.

## Regression suite (vs 1.8.45)

- S1 pull-release-roll: qMin worst −21.0 → −6.8. Pitch-up departures 4 → 0. qRms 9.49 → 4.96. t90 1.43 → 1.19 s (n90 53 → 60).
- S2: qRms 6.09 → 4.98, AoA peak max 33.8 → 29.5.
- S3/S7 1g rolls: bank angles identical. Pitch rate during the roll −0.5..0.4 → −0.9..0.6 deg/s at 450-800 km/h.
- S4 full pull + roll at 350 km/h (roll started at ~42 deg while pitching at 49 deg/s): bank at 1 s 60 → 51. Across four roll timings it is 3-9 deg lower. The other S4 cases are within ±5.
- S5, S6: within noise.
- Asymmetric store (single wing):
  - 600 kg: the jet rolls the commanded way sooner at 330/450 km/h (bank at 1 s 1-3 → 11-12 deg). At 600 km/h, AoA at release is 44 → 38.
  - 1500 kg: within noise.
- AI heading changes of 90/150/170 deg: identical.

## Telemetry

New columns: `alphaCmdRaw`, `nCmdRaw` (`alphaCmd`/`nCmd` are now the shaped references), `rateBlendAoA`, `hiRate`, `rateW`, `zoneX`, `authCap`, `authCapEff`, `nLiftMax`, `hybridDMacc`.

## Not verified in game

All of it is bench-only. Three things to check first:
- The feel of the rate zone.
- Whether the idle cap (~26 deg on the bench) is too tight.
- The 1 g G-step speed.

Bench additions: `prof=t:v,...` stick profiles, `push`/`pushT`, `storePart=A+B`, and `DUMPDN=1` (authority curves).
