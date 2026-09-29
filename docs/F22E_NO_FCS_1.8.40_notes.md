# F-22E Strike Raptor FCS 1.8.40 (Nuclear Option)

1.8.39 plus: a rework of the nozzle kinematics, stock nozzles for AI jets, and roll–pitch priority in the FCS. Both DLLs get all three.

## 1. Nozzle kinematics (visual only)
Reworked from your F119 footage and sketches. The kinematics are in `NozzleKinematics.cs`.

**Pieces** (mesh regenerated: 1020 vertices, 664 triangles; same count after the root and skin reshaping, against 840 / 568 in 1.8.39 and Aryx's 468 / 400)
- **Root (blue):** fixed, under the shroud.
- **Cowl skin (green):** only the forward segment's outer skin, including Aryx's 1 cm step at the crease. It floats between root and flap, overlapping both (the overlapping-panel look is kept).
- **Flap (purple):** the end panel, plus the forward segment's inner wall back from the throat. So it is one piece from the throat back.
- **Removed:** the forward segment's side panels. It's open under the skin, as on the real nozzle.
- **Gap infill** (148 triangles, all dark, all one-sided and facing the way they are seen from outside):
  - **Box beam under each side edge of the floating panel** (dark grey), modelled on your F119 close-ups: the panel is bolted on top of a square tube and the cavity behind it is dark.
    - **Size and place:** 4 × 4 cm, 1.7 cm under the skin, with its outer face at |x| 0.428 m (2.4 cm inboard of the panel edge). It is rigid with the panel.
    - **Length:** it runs from 10 cm behind the panel's front edge to the panel's back edge, with square end caps.
      - Starting it further forward puts it into the root at +20°.
      - Continuing it aft into the flap, as in the photo, crosses the flap's outer chamfer (outward swing) or its inner surface (inward swing) at every length and dip I tried, so it stops at the panel's back edge.
    - **Bolt tabs:** two mid-grey tabs on its outer face under the panel edge.
  - **Black backdrop:** two plates at |x| 0.37 m.
    - The upper one hangs 9 cm from the skin liner and is rigid with the skin. Its front corner is cut back so it clears the root.
    - The lower one runs from the flap's inner wall up 9 cm, and aft under the gusset to z −0.62 m, rigid with the flap. The first cut of this build stopped at −0.53 m; from slightly below you could see through under the gusset's front edge.
  - **Removed:** the hanger links, flap rail and lug from the previous build, and the earlier edge beams.
  - **Skin liner (black):** 1.5 cm under the skin, facing inward, out to |x| 0.448 m, just above the beams. It hides the skin's culled underside and the gap between the beam and the skin.
  - **Flap floor (black):** 1 cm above the flap's inner wall, from the throat to the end panel, facing outward. It hides the inner wall's culled back face, so the engine glow no longer shows through the gaps.
  - **End caps (black):** on the root's aft face and the flap's front face.
  - **End-flap sides:** Aryx's side faces of the end flap are remapped to the atlas's dark grey (a plate, as in your photo), with two black hexagonal lightening holes, 4.4 cm across, 0.5 mm proud of the plate.
- **Side seals from the earlier 1.8.40 build:** removed.

- **Cavity materials (latest build):** the dark parts no longer use Aryx's texture atlas.
  - **Why they looked light grey:** Aryx's skin shader draws the livery texture over the base colour. The "black" spot I had picked is black in the base colour but mid grey (90/255) in the livery. The dark grey spot is 49/255, and the livery has no dark area large enough to survive mipmapping. All of it also picked up the skin's metallic 0.41 / smoothness 0.44 sheen.
  - **Now:** the mesh has 4 submeshes. Aryx's skin material is on everything original. Three plain URP/Lit materials carry the rest, each with metallic 0 and smoothness 0.15:
    - black (0.035): liners, backdrop, floor, caps, lightening holes;
    - dark grey (0.085): beams and the end-flap side gussets;
    - grey (0.20): bolt tabs.
  - **Where they come from:** clones of the plain URP/Lit material Aryx's bundle already uses, with only colour and smoothness changed, so no new shader variants. If that material isn't found it falls back to `Shader.Find`, and failing that to Aryx's material (the old look). The BepInEx log says which.
  - **Trade-off:** the dark parts don't show battle damage or liveries.

**Area schedule**
- **MIL:** flap and skin rotated 7.7° in about the skin's front edge. The skin sits flush with the flap.
- **Idle:** the flap moved out 4 cm, with the skin ramping up from the root's edge onto it. At 4 cm the flap's side edge comes up to about level with the sidewall edge.
- **AB:** as idle, plus a 1.5° flare about the throat.
- **In between:** blended.

**Vectoring:** the flap rotates about the throat, where its inner surface meets the root. Same pivot both ways.
- **Flap swinging outward:**
  - **Pull:** root, skin and flap are pulled in together by 0.47 m × tan(δ/2), forward and 8° outward. That is 2.1 / 4.1 / 6.2 / 8.3 cm at 5 / 10 / 15 / 20°. It's the offset that keeps the exit width across the turned flow (a mitred bend, as in your sketch).
  - **Why 8°:** it's close to the sidewall's top edge, which rises at about 10° beside the root, so the root stays within about 0.25 cm of the wall. A steeper pull puts the root's front side corner through the top of Aryx's thin shroud lip.
  - **Skin:** it rotates with the flap, and its front edge swings forward over the root. It is also tilted up about its back edge, 0.35 cm at 10° and 0.64 cm at 20° at the front. That lets it ride over the flush root instead of cutting into it.
- **Flap swinging inward:** the skin floats, centred between the root's edge and the flap's front. Its angle is the area angle plus half the flap's rotation, which leaves two even gaps. The beams and black caps show through them.

**Root and skin shape changes (from your photo of the slanted sidewall):**
- **Flush root:** the root's outer side edge now runs along the sidewall's top line. One vertex per side, just under the shroud lip, is raised from 0.413 to 0.450 m. Aryx's root was flat and sank up to about 3 cm below the wall at the lip. It is now within 0.06 cm of the wall at z −0.10 and within 0.4 cm at the lip.
- **Tapered skin front edge:** the skin's front bend-down is Aryx's full depth at the centre and flat at the side edges. At the side edges it used to be 0.7 cm deep.
  - Neutral idle is unchanged.
  - At MIL the skin's front stands up to 1.1 cm above the root at the outer edges; it was 0.4 cm. The seam strips fill the step.

**Checks**
- **C# vs Python:** the C# kinematics matched the Python model to 0.0005° and 0.0005 cm before the pull-in was added. The clipping checks below pose the mesh with transforms dumped from the C# code itself.
- **Clipping:** 5 area settings × 9 nozzle angles (−20…+20°).
  - **Engine, the other flap, and the flap against the shroud and sidewalls:** 0 intersections.
  - **Root and skin against the shroud: they overlap (831 triangle crossings over the grid).** It's the root's front and the skin's front edge going into Aryx's shroud. They do that at neutral too, because the flush root reaches into the lip. It's hidden: the closest any root, skin, liner or beam point comes to the shroud's outer surface is 0.56 cm under it, at the root's front side corner at +20°, MIL.
  - **Skin against root:** the skin's front never goes below the root's top at +10 or +20° (checked on 31 sections). At MIL neutral it dips 0.33 cm; Aryx's original did the same.
  - **Cavity parts:** 0 crossings with the flap surfaces. The black liner and the upper backdrop cross into the root under the skin's front (hidden, as before).
  - **Sidewall contact:** parts at the sidewall are checked 1 % inboard, the holes 0.1 %. Aryx's flap sides and the sidewall's inner face are coplanar within about 1 mm.
- **Dark parts above the outer surface:** none, sampled on a 15 × 26 grid per flap per pose.
- **Render check:** z-buffered offline render with back faces culled, from the side, slightly aft, slightly below and above. Picture: `F22E_nozzle_1.8.40_pull_flush.png`.
  - No see-through at 0, ±10 and ±20°, idle and AB, with one exception.
  - **Exception:** from slightly below at idle 0 and +10°, a thin white sliver shows along the skin's lower side edge. It's the skin's own back face at its chamfered edge. The installed previous build had exactly the same sliver.
- **Joints:** not re-measured for this build. In the no-pull build, where the skin meets the root and the flap, the deepest overlap was 3.3 mm and 6.3 mm on a 9 × 50 point grid per pose.

**Earlier builds today:** superseded by this one. That covers the build where only the end segment moved, the build with the flap pulled into the cowl, and the side-seal build.

**AI jets:** new key `6. Visuals / NozzleAnimationAI` (false). AI F-22Es keep Aryx's one-piece nozzle: no skinning and no per-frame posing. Jets with a player, including other players online, are animated. The check runs once a second, so a jet whose pilot changes switches over by itself.

## 2. Roll–pitch priority (`4. Gains / RollPitchPriority`, true)

**What your 2026-09-25 log shows**
- **385 km/h, 6° AoA, part throttle:** a 100 °/s roll moved the collective stab 8° nose-down, and the nozzles 1.7° off their trim.
- **275 km/h at AB:** the nozzles moved 2.5–3.5° off trim during rolls.

**What the bench shows as the cause**
- **The flaperons:** their pitch moment is not in the allocator's model; it is fed forward instead. Full differential at moderate AoA pitches the jet up by 24–76 °/s² as the TE-up side loses lift.
- **The stabs:** they are modelled with a monotone lift envelope. In a hard roll the down-going stab is driven to 30–55° local AoA, past its lift peak. There its nose-down authority is gone (your "reversal" point), so the nozzles take up the pitch, to the −20° stop at 15–20° AoA.

**The fix:** a second allocation pass while a roll is commanded.
- **Nozzles:** held within `RollTvcBudget` (1.5°) of their pre-roll trim.
- **Surfaces:** the flaperons' true pitch moment is in the model. The stabs are kept on the attached side of their lift peak.
- **Weights:** pitch and yaw count 10× over roll, so roll rate is what gives.
- **Acceptance test:** the pass is used only if its modelled pitch error stays within 4 °/s² of what free nozzles would give. Otherwise the band widens (×3, ×8) or the normal allocation is kept.
- **When the nozzles are let go:**
  - In AoA protection, or on overshoot past the AoA or asymmetric-load limit.
  - Briefly on roll stops and reversals, where the roll's pitch moment swings faster than the stabs can follow.
- **Fade-out:** the pass fades out between 18 and 26° AoA; above that the nozzles are free as before.

**Bench, 1.8.39 → 1.8.40**

| Case | Nozzle deviation from trim during roll | Roll | Pitch |
|---|---|---|---|
| 1 g full roll 300–750 km/h, part throttle | max 1.5–3.4° → 1.5–2.3° | unchanged | unchanged |
| 1 g full roll 250–750 km/h, AB | max 2.9–6.7° → 1.5–1.9° | unchanged | unchanged |
| 1 g full roll 250 km/h (19° AoA) | max 16° → 2° | bank at 2 s 45° → 29° | — |
| Held ~19.6° AoA + roll, part throttle | mean 15–16° (at the −20° stop) → 1.8–2.8° | bank at 0.8 s −15…−25 % | q error similar (0.5–2.9 °/s) |
| Held ~19.6° AoA + roll, AB | mean 8–15.5° → 4.7–7.6° | bank at 0.8 s about −3…−11 % | similar |
| 3.4 t asymmetric store, full aft + roll, at the cap, part throttle | mean 5.3–7.3° → 1.8–3.0° | unchanged | α peak 13.4–14.4° → 12.0–12.6° (cap 12); q error 3.0–6.5 → 0.6–1.7 °/s |
| Same store, full aft + roll, AB | mean 6.8–12.1° → 4.6–6.0° | unchanged | α peak 12.0–13.6° → 11.9–12.2° |
| Standard suite, S1 pull–release–roll | — | — | qMin worst −24 → −21 °/s; q rms 10.6 → 9.2 |

**Asymmetric-load departure set** (3.4 t at 2.32 m, 250–550 km/h, 4 manoeuvres × 2 sides):
- **0.6 and full throttle:** 0 departures before and after.
- **Idle:** unchanged. The same 8 cases at 250 km/h are out of envelope, as in 1.8.37.

**What it costs / what got worse**
- **Roll rate at moderate AoA and low speed.** 15–25 % in held-AoA rolls at ~20° AoA. 35 % in a 1 g roll at 250 km/h, because the bench jet needs 19° AoA there. Nothing at 1 g above ~300 km/h.
  - Raise `RollTvcBudget` to trade nozzle travel back for roll rate.
  - `RollPitchPriority = false` gives 1.8.39.
- **Asymmetric store, full aft stick, AB, full-deflection roll spam at 400–600 km/h:**
  - AoA overshoot above the cap: mean 2.9 → 3.5°, worst 4.9 → 8.4° (36 cases).
  - Cause: the stabs now carry the trim, so they have less reserve left when a reversal swings the pitch moment. The nozzles are let go on reversals, but they slew from near trim instead of starting from −20°.
  - No departures in the set.
- **Nozzles on reversals:** they still move briefly on roll reversals. That is by design; see "When the nozzles are let go" above.

**Telemetry:** two new columns before the surface columns:
- `rpGate`: 0–1, how active the pass is.
- `rpBand`: nozzle band used in degrees; −1 = normal allocation kept.

## Not checked
- **Nozzle, in game:** I haven't seen this build in the game.
  - The one-sided infill faces are wound the same way as Aryx's faces, which render in game. I have not confirmed it in game.
  - The atlas's dark grey and black are the only colours used. Whether the gusset reads like your photo under the game's lighting is untested.
- **FCS pass:** the bench only; I have no in-game log of it yet. The asymmetric-load numbers use a bench store at Aryx's pylon geometry, not your real loadout.

## Accurate positive-deflection pull (done in this build)
- **Formula:** p = 0.47 m · tan(δ/2), forward and 8° outward, as above. Throttle doesn't enter it, because Aryx's flaps converge to the exit at every throttle setting.
- **Earlier builds:** 2 cm at 20° straight forward left the exit about 3 % narrower at 20°.
- **Directions I tried and dropped:**
  - Straight forward: the wall ends up 1.7 cm above the root at 20°.
  - 13.5° outward, along the wall further aft: the root's corner goes 0.5 cm through the top of the shroud lip.
