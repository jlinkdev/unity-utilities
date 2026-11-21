# Procedural Plants 2.2.1 architectural review

The package has three kinds of state with different lifetimes. A growing sim can advance and save the botanical record without a scene. A living component also owns an environment, optional soil simulation and a generated mesh. Windowsill owns the two pot positions, selection, tray and comparison clock. Keeping those responsibilities separate lets the same seed and time sequence run in the Inspector, in a game or offline.

| Boundary | Owner | Responsibility |
| --- | --- | --- |
| Botanical structure | `PlantGrowth` | Stable organ IDs, biological age, births, pruning and plantlet removal. Save parsing and migration live in its persistence partial. The growth model owns stem extension math used by both the mesh and the growing-tip guide. |
| Species traits | `PlantSpeciesProfile` | Display name, vine capability, organ capacity, maturation time, soil drying and wilt response. The enum has explicit save IDs. Species-specific shapes and branching rules remain in the growth and mesher files. |
| Living specimen | `ProceduralPlant`, `PlantCare`, `PlantSpecimen` | The component applies the host's clock, owns the mesh and combines growth with its environment. A specimen snapshot carries growth, care, conditions, supports, detail and surface. Project material overrides travel with Unity instances and prefabs. |
| Geometry | `PlantMesher` | Builds both submeshes and attributes from the growth record. Leaf ranges identify which vertices may wilt. A plant's mesher retains its hydrated positions and colors so a care update can restore exactly that pose. |
| Sample and editor | `WindowsillWorkshop`, component Inspectors | Select donors, destinations and actions. Windowsill checkpoint decoding and preflight checks live separately from its IMGUI controls. |

## Findings and changes

1. **Soil changes could rebuild a mature plant with no visible change.** A runtime revision identifies structural changes, including a caller cutting a branch through the exposed growth record. Advancing an unchanged plant now compares its displayed droop with the new value. A change in droop updates positions, colors, surface vectors and bounds while reusing triangle indices and UVs. Watering, disabling care, switching mesh detail and re-enabling a dormant object are covered by mesh comparisons.
2. **Propagation and copying repeated state transfer.** Windowsill previously serialized the donor, discarded its growth string, and serialized the offspring. Cuttings, plantlets and variations now pair their own growth record directly with the source settings. The component Inspector creates a living cutting from that record before enabling its renderer. Forecasting a particular vine also copies the in-memory organ and bud lists directly.
3. **The care inheritance rule was implicit.** Windowsill copies the donor's care toggle and soil level at separation. Each pot owns its copy from that point. Watering the new pot has no effect on its mother. This preserves v2.2.0 behavior and allows a game to choose fresh soil by watering the destination when planting. Loading a full snapshot restores recorded moisture. Loading structure alone leaves the destination environment in place.
4. **A malformed tray could replace the mother before failing in the UI.** Windowsill now checks all populated specimens, tray completeness, pending cutting type and saved placement before touching the displayed pots. A regression test restores a partially filled tray over a different mother and verifies that the mother survives the rejected load.

## Shelf measurement

Windows x64 Development Player, Built-in pipeline, Unity 6000.0.58f1, Intel Core i7-10700K. The same workload ran against v2.2.0 and v2.2.1. The shelf held 18 mature plants, three of each species, one crown each, Standard detail, 154,192 vertices in total. Each operation advanced or watered all 18 consecutively on the main thread. Two warmups preceded 16 measured frames per operation. The timings measure synchronous plant work. GPU rendering and whole-frame time are outside the timed region.

| Operation across 18 pots | v2.2.0 median | v2.2.1 median | v2.2.1 p95 | Median frame GC bytes, before → after |
| --- | ---: | ---: | ---: | ---: |
| Dry hydrated soil by 0.125 units | 91.975 ms | 0.005 ms | 0.008 ms | 2,448 → 0 |
| Dry already stressed soil by 0.125 units | 120.289 ms | 71.418 ms | 74.300 ms | 2,448 → 0 |
| Water wilted plants to full | 92.151 ms | 47.344 ms | 58.627 ms | 3,312 → 0 |

Unity's **GC Allocated In Frame** counter reported zero bytes for idle frames and 74,304 bytes for a control that allocated 18 arrays of 4,096 bytes. The reported zeros apply after warmup. A wilted plant retains scratch buffers for positions and colors, about 16 bytes per vertex before list capacity overhead. Structural growth still rebuilds the topology. The profiling harness is included for reruns in a Development Player. [ShelfProfile.cs](../Tests~/ShelfProfile.cs)

## What to tackle next

For the next species, assign a stable ID, supply its profile, implement its birth and mesh rules, then add topology and save coverage plus a Windowsill preset. The six current species have distinct geometry. An external species registration system would add a new persistence contract and is unnecessary for adding another built-in species.

For a room-scale sim, distribute visible wilt updates across frames before allowing the whole shelf to update together. The measured 71 ms for simultaneous wilt changes exceeds a 60 Hz frame budget on this machine. CPU mesh normals and tangents are recalculated when the leaf pose changes. Capture a Unity Profiler trace on the target hardware before choosing a more invasive rendering change. [Verification record](validation.md)
