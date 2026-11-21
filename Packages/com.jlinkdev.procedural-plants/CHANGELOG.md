# Changelog

## 2.3.1

- Softened the waxy highlights on jade, varied the cream margins on rubber leaves and reduced vein and marbling contrast on pothos.
- Updated Windowsill with softer daylight, narrower window bars, textured soil and shelf contact shadows that follow the pots.
- Added **Selected pot** and **Both pots** framing in the Windowsill Inspector and Game view. Close-up follows the selected plant as it grows. Starting a climbing and trailing comparison frames both pots.

## 2.3.0

- Added seeded jade plants with thickening stems, side branches and opposite pairs of fleshy leaves. The closed blades include UVs, a waxy PBR surface, tinted margins and three mesh detail levels.
- Added **Jade shoot** to Windowsill in Edit and Play modes, and jade to the Plant Studio selector. The preset starts before the first fork so its development is visible with two growth steps.
- Added regression coverage for branch attachment, paired leaves, deterministic continuation, optional care, topology and mesh baking.

## 2.2.1

- Centralized shared species traits and pinned the six serialized species IDs. The visible vine-tip guide and stem meshes use the growth model's extension calculation.
- Separated structural mesh builds from soil and leaf-pose updates. Watering restores the hydrated geometry and keeps mesh baking available at the current pose.
- Unified specimen transfer for plantlets, cuttings and variations. Windowsill checks specimen settings and rejects incomplete trays before replacing either pot.
- Added regression coverage for independent care after propagation and reload, full mesh equivalence under changing care, and a repeatable Windows player shelf profile.

## 2.2.0

- Added spider plants with striped arching leaves, runners and detachable juvenile rosettes. Detaching a mature plantlet preserves its leaves and records the removal on its mother.
- Added rubber plants with woody stems, leaf scars, red stipules, and glossy deep-green or variegated leaves. Both species have closed leaf meshes at all three detail levels and bake with UVs.
- Added Windowsill presets and a plantlet selector in Edit and Play modes. A selected plantlet roots in pot 2 and grows independently.
- Added optional care to each specimen. Soil dries with the growth clock, water stress slows development and wilts leaves, and watering restores them. Living snapshots and bench saves retain care settings and moisture.

## 2.1.0

- Added live cutting placement with yaw, tilt, adjustable pole position, and trailing and climbing presets.
- Added active-bud and contact guides driven by the same growth calculation as the next birth. Select a vine to inspect its next extension under the current conditions.
- Added propagation from mature Monstera nodes with developed leaves and aerial roots. The cutting and donor each activate a bud.
- Added a shared-clock Windowsill comparison for two independent copies of the same cutting. Checkpoints include placement and comparison state and can restore a 2.0 bench.

## 2.0.0

- Added persistent rim, shelf and climbing-pole contacts for newly grown vine segments.
- Added stable node selection, pothos pruning, parent bud regrowth and independent cuttings. Version 1 saves acquire node IDs when loaded.
- Added Monstera with aerial roots, juvenile blades and progressively split and perforated mature leaf meshes at all three detail levels.
- Added the two-pot Windowsill sample with Edit and Play mode controls, six-seed variation tray, pinning and bench checkpoints. Bake living or static prefab assets in the editor or create independent copies at runtime.

## 1.1.0

- Added Built-in and URP scene-lit PBR materials with base color, tangent-space normal and packed occlusion, roughness and metallic textures.
- Added curved pothos internodes, unequal shoot growth rates and varying leaf orientation. Existing growth records retain their established stems.
- Added the Long pothos preset and a material comparison control in Plant Studio.

## 1.0.0

- Added seeded pothos vines with axillary buds and folding heart-shaped leaves, variegated snake plant rosettes, and arching fern fronds with paired leaflets.
- Added editable planting recipes, age-based mesh generation, directional growth, moisture-dependent development and versioned JSON persistence.
- Added an edit-time and runtime component, Unity mesh baking with UVs, and the interactive Plant Studio sample.
- Added three leaf mesh detail levels, closed tapered blades with shared margins, and a foliage wireframe inspection view.
