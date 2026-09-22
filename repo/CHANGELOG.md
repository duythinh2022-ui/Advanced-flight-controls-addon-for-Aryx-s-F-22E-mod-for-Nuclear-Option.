# Changelog

Bench data for every entry is in `docs/F22E_FCS_<version>_NOTES.md`. Unless noted, results are from the 6-DOF
bench, not from flight tests.

## 1.7.6
- Fixed the nose-down shove in tail slides with the protection override held. In reversed flow (the stab's own local
  AoA past 120°) the stabs' pitch model now follows the reversed deflection sense instead of being flattened to zero
  authority. The rudders fade back in from 120° to 140° AoA. Bench: nose-down rate while held went from up to
  −84 °/s to within −0.9 °/s in all 18 tail-slide cases. Releasing the stick recovers nose-first in 3.5–7.2 s.

## 1.7.5
- AoA-protection override: after the recovery, the jet no longer stays frozen at zero pitch rate while you keep
  pulling. The latch now releases once AoA is 5° back inside the limit, and the normal AoA law takes over. It arms at
  85 % stick; 90 % counts as full opposition. The reported nose shove at 110–130° AoA could not be reproduced on
  the bench (telemetry CSV requested).
- Takeoff rotation law: with the mains on the ground and the nose wheel up, pitch is rate-commanded (full stick
  `RotationRate` = 8 °/s) and neutral stick holds the nose attitude. The nose-wheel-down ground roll is unchanged
  (direct).
- Buffed build only: 20 mm gun ammunition ×2 (`GunAmmoScale`, extra rounds add their mass through the game's rearm
  path) and fuel consumption −25 % at every throttle setting (`FuelConsumptionScale` 0.75).

## 1.7.4
- Roll-reversal hitch removed. A reversal is planned at one constant acceleration, with no damping-assisted peak that
  the jet then lags. The new roll starts with that same acceleration, so there is no step at zero.
  Roll-acceleration sag at the zero crossing is 0 % at 90–300 m/s (1.7.3: 1–10 %), with the same reversal times.

## 1.7.3
- Fixed the hitch in instant roll reversals. The stop shaping (which returns the surfaces to neutral at zero rate so
  a stop does not rebound) also applied when reversing through zero, so the surfaces swung toward neutral and back.
  Through a reversal the planned acceleration now stays at or above what the new roll starts with. Roll-acceleration
  sag at the zero crossing: 31–38 % → 4–10 % at 90–160 m/s. Stops are unchanged.

## 1.7.2
- The TVC no longer goes negative in low-AoA rolls. Below ~150 m/s IAS the nozzles catch the roll's pitch transient,
  and its sustained part is washed out onto the stabs, as far as the stabs have travel to spare (two-pass
  allocation). Above ~150 m/s IAS the nozzles hold still. Mean TVC during a full roll at 80–130 m/s: −3…−4° → +0.7…+3°
  (near the pre-roll trim). Pitch disturbance is unchanged within ~0.6 °/s.
- `StabilatorRollShare` 0.6 → 0.55, which leaves the stabs pitch headroom: −4 % / −2 % roll rate at 80 / 100 m/s.
- `RollNozzleHold` is now a 0–1 strength (default 1). New `RollNozzleWashout` (0.4 s).

## 1.7.1
- Stabs work harder in low-IAS / low-AoA rolls: `StabilatorRollShare` 0.45 → 0.6, `StabilatorRollCost` 3e-3 → 5e-4.
  Roll rate at 80 / 100 m/s TAS goes 117 / 152 → 138 / 174 °/s, and the wing surfaces run at ~0.5 of their travel
  instead of 0.7–0.95. Jerk per unit of roll rate and roll-entry time are unchanged.
- New `RollNozzleHold`: above ~125–150 m/s IAS, the stabs (not the nozzles) absorb the pitch moment that low-AoA roll
  deflections create, with no pitch-rate cost. Below that IAS the nozzles keep doing it, because they are faster there.

## 1.7.0
- **Two builds.** *Buffed* (thrust ×1.08, parasitic drag ×0.885, flares ×10, EW storage ×1.5 / recharge ×1.2) and
  *Stock performance* (original thrust, drag and countermeasures; countermeasures not touched at all). The control
  laws and aero/control tweaks are the same in both. The stock build has its own config file. No law retune was
  needed for stock thrust: the FCS measures its authorities live, and the full regression differs by 1–3 %.
- **Low-IAS yaw-rate margin is now a curve.** The full `LowIasYawBoost` (×1.35) applies up to 48 m/s IAS and
  decays exponentially above it (×1.17 at 55, ×1.04 at 70, ×1.01 at 83 m/s). New keys `LowIasYawBoostFullIAS` and
  `LowIasYawBoostFalloff`. Falling-leaf yaw rate is kept (24.5 / 25.6 °/s). High-AoA roll stops at 55–85 m/s IAS
  are 0.2–0.4 s quicker.
- **Yaw feed-forward is full at moderate/high AoA.** `YawRateFeedForward` (0.5) now only applies below 15° AoA,
  blending to 100 % by 28°. Roll stops at 25–28° AoA: 1.16–1.32 s → 0.92–1.16 s, with less sideslip.
- **AoA-protection override fixes:**
  - AoA is unwrapped through ±180°. A tail slide no longer flips the protection to the negative side, which used to
    shove the nose down at −25 °/s.
  - The override (full stick against the protection) is latched and holds zero pitch rate until the stick is eased,
    also once AoA falls back inside the limit.
- Known: a rolling pull at the AoA limit at 170 m/s peaks at 67° (was 65°) before protection catches it.

## 1.6.5
- `LowIasYawBoost` (×1.35 below 120 m/s IAS): falling-leaf yaw 20 → 24–26 °/s.
- `FcsApi` read-only API (pitch law, commanded AoA/G, protection) for other plugins.

## 1.6.4
- `MaxRollRate` 280 → 250 °/s, `MaxYawRate` 65 → 75 °/s.

## 1.6.3
- Actuator damping is now an acceleration limit (full servo speed kept) instead of a first-order lag. Stabs and
  TVC are quicker than in 1.6.2.
- Fixed the TVC sitting slightly negative after high-AoA manoeuvring (allocator Levenberg damping 5 % → 0.1 %).
- `MaxRollRate` 420 → 280 °/s. Roll lead retuned.

## 1.6.2
- Roll-stop fix: braking budget = wing surfaces + roll damping. This removed a −10 °/s rebound at low IAS and the
  slow tail at high IAS.
- Roll lead (`RollLagCompensation`), servo-limited roll jerk, 30 ms actuator damping.
- Countermeasures: flares ×10, EW storage ×1.5, recharge ×1.2.

## 1.6.1
- Rudder/yaw fix: yaw axis priority ×8 below ~20° AoA, and 50 % yaw feed-forward below 200 m/s IAS. Coordinated
  rolls at all speeds, no rudder hunting.
- Extra load-limiter lead while rolling.

## 1.6.0
- Differential-stab roll help at low IAS (roll rate +20–30 % at 95–185 m/s TAS).
- Nose wobble in roll spam damped (sideslip and yaw gains).
- `YawStopFactor` 0.45 → 0.7 (moderate-AoA roll response).
- Outer flaperons droop with the flap schedule.
- 20 ms actuator damping and 60 ms stick smoothing.

## 1.5.0
- Relaxed-stability re-balance: stabilator ×1.30 → ×1.10, LERX ×1.6, CG +0.05 m aft. The neutral point moves from
  0.51 m to 0.09 m aft of the CG. Trim at 21° AoA: stab 9–10° + TVC 10° → stab 4.7° + TVC 1°.

## 1.4.0
- Roll ceiling tied to the original aircraft's roll rate (re-derived from the stock FBW), +20 % / +10 %, limited by
  measured authority.
- Roll/yaw surfaces removed from pitch allocation.

## 1.3.0
- Jerk-limited velocity-vector roll trajectory with fed-forward acceleration.
- Yaw rate capped by measured yaw authority. Rudders fade out 35–50° AoA.
- AoA law is now dynamic inversion with an integrator. FCS takeover of the flap schedule.

## 1.2.x
- First companion build: AoA/G blended pitch law, protection, MPO, gear-down rate law, INDI plus a stall-aware
  allocator, pitch-only TVC, effectiveness and servo scaling.
