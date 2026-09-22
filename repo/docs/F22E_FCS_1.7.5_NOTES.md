# F-22E Strike Raptor FCS 1.7.5 (Nuclear Option)

1.7.5 is 1.7.4 plus two bug fixes (AoA-protection override, takeoff rotation) and two buffed-only additions (gun
ammunition ×2, fuel burn −25 %). Every other bench result is identical to 1.7.4. That includes the 60 s random-stick
stress test and both builds.

## 1. AoA-protection override: stuck at zero rate after recovery (fixed)
The 1.7.0 latch held zero pitch rate until the stick was eased below 90 %. If you kept full aft stick the whole time,
the jet stayed frozen at q = 0 even once AoA was back inside the envelope. That was the "0 rate even when I'm
pulling" bug, and 1.7.0 caused it.

Now:
- The latch **releases on its own once AoA is 5° back inside the limit** (below 60° for the +65° limit). The normal
  AoA law then takes over, so full aft stick pulls back up to 65°.
- It arms at 85 % stick (was 100 %), and 90 % stick already counts as full opposition in the un-latched override. A
  stick that stops a little short of the end (deadzone or curve settings) still holds the nose.
- It still releases if you ease below 70 %, and on any mode change.

T38 (MPO past the limit, Stability Assist back on, full aft held; 90/130/170 m/s, AB and idle, switch at 50–123° AoA):

| | 1.7.4 | **1.7.5** |
|---|---|---|
| Time frozen at 0 rate after AoA is back inside the envelope | until stick eased (whole run) | **0.0 s** in all 18 cases |
| Pulls back toward the limit with the stick still aft | no | **yes** (the exceptions are cases where AoA never left the limit region, e.g. idle at 90 m/s, where the jet stays pinned at ~57–65°) |
| Pitch rate while latched (AB) | −0.1 … +0.6 °/s | same |

### The nose-down shove at 110–130° AoA: not reproduced
I tried MPO entries at 90–170 m/s, AB/100 %/60 %/idle, switches at 50–123° AoA, and a new tail-slide test (T40:
pull to vertical, bleed to <40 and <15 m/s, full aft held). None of them shows a nose-down rate above 5 °/s while the
override is latched with the stick fully aft. The one known physical case is still there. At idle thrust and low
q̄, the nose sags up to −11.8 °/s at 55–80° AoA even with zero commanded, because the pitch-up authority is not
enough.

Possible in-game causes the bench cannot show: the stick not reaching 85 % (curve/deadzone), transonic or ground
effects, or a sim/game difference at >100° AoA. **If it still happens, please send a telemetry CSV
(`Telemetry = true`).** The `prot`, `sp` (stick), `alpha` and `qT` columns will show which it is.

## 2. Takeoff rotation: nose attitude hold (new)
On the ground the pitch law was direct (stick = surface), so between nose-wheel lift-off and main-wheel lift-off
nothing held the attitude. It rose or fell with airspeed and trim.

New law: when the **main wheels are on the ground, the nose wheel is off, and TAS > 15 m/s**, the FCS switches to
pitch-rate command.
- Full stick = `RotationRate` (8 °/s, new key). Neutral stick holds the current nose attitude.
- It stays active until the nose wheel is back down for 0.3 s, the mains leave the ground, or TAS < 12 m/s.
- After lift-off the normal gear-down law takes over (as before).
- With the nose wheel down (taxi, start of the roll), the law is still direct, like stock.

The nose gear is found at spawn: it is the leg ≥1.5 m ahead of the others. The log prints
`F-22E FCS: nose gear '<name>' …`. If no distinct nose leg is found, the rotation law stays off and you get the 1.7.4
behaviour.

T41 (bench, 60 m/s, rotation):

| | Nose wheel down (direct) | **Nose wheel up (rotation law)** |
|---|---|---|
| Half stick for 1 s | q 26.5 °/s | **q 4.1 °/s** (½ × 8) |
| Stick released | q −10.6 … +14.5 °/s, attitude drifts 42.1 → 43.4° | **q −0.2 … 0.0 °/s, attitude 10.1 → 10.0°** |

Verification limit: the bench has no ground reaction forces, so the wheel contact is simulated by flags. The law
itself is the same pitch-rate/INDI path as the gear-down law, which is well tested in the air.

## 3. Buffed build: gun ammo ×2, fuel burn −25 %
| Key | Buffed | Stock perf | Effect |
|---|---|---|---|
| `GunAmmoScale` (new) | 2 | 1 | 20 mm magazine capacity. It applies to the load at spawn and to what rearming refills to. |
| `FuelConsumptionScale` (new) | 0.75 | 1 | Scales every fuel burn call (all throttle settings, afterburner included), all F-22Es |

- **Weight:** yes, the extra rounds weigh something. The top-up goes through the game's own rearm path
  (`WeaponStation.Rearm`), which adds `massPerRound × rounds` to the gun hardpoint's mass, the same way a normal
  rearm does. I did not read the Aryx gun's round count and per-round mass. For
  scale, 480 real M61 rounds weigh ~125 kg. The gun sits near the CG, so the pitch trim shift is small.
- **FCS:** fine. The FCS re-reads mass and inertia periodically, and the INDI inner loop works from measured
  acceleration, not from a mass model. The G and AoA limits are unchanged. The only effect is a little more weight,
  so you get slightly less excess power, the same as carrying a bit more fuel.
- Fuel: the tank size is unchanged. Endurance and range go up by ×1.33.

Verification limit: both paths are only compile-checked against the game assemblies. The bench does not model
weapons or fuel burn. Check the log line `F-22E countermeasures / ammo on …` at spawn and the ammo count on the HUD.

## Config
| Key | Default | Note |
|---|---|---|
| `RotationRate` | 8 °/s | new (Envelope section) |
| `GunAmmoScale` | 2 buffed / 1 stock | new |
| `FuelConsumptionScale` | 0.75 buffed / 1 stock | new |

No migration: the new keys are added with their defaults, and existing values are kept.
