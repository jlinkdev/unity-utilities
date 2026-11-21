# Runtime and mesh guide

## Plant a specimen

A `ProceduralPlant` creates a local-space mesh and owns its lifetime. Its origin is the soil surface and local Y is up. Parent it under an ordinary planter, or under a soil anchor in a Procedural Pots scene. The plant package has no dependency on the pot package.

```csharp
using JLink.Plants;
using UnityEngine;

public class Windowsill : MonoBehaviour
{
    [SerializeField] ProceduralPlant plant;

    void Start()
    {
        var recipe = PlantRecipe.Default;
        recipe.species = PlantSpecies.Fern;
        recipe.seed = 214;
        var conditions = PlantConditions.Default;
        conditions.lightDirection = plant.transform.InverseTransformDirection(Vector3.right);
        plant.Conditions = conditions;
        plant.Generate(recipe, initialGrowth: 8);
    }

    // The host chooses what a unit means. Call from its clock, care system or UI.
    public void PassOneDay() => plant.Advance(1);
    public string Save() => plant.SaveState();
    public void Resume(string json) => plant.LoadState(json);
}
```

For a distant or unloaded plant, use `PlantGrowth.Sow(recipe, conditions)`, call `Advance(units, conditions)`, then `ToJson()`. Restore with `PlantGrowth.FromJson(json)`. To draw a standalone state, call `new PlantMesher().Write(state, mesh)` on the Unity main thread. The caller owns that mesh. Reuse the mesher and mesh when updating. A component's `Growth` exposes the same state. Prefer advancing through the component so it refreshes the visible mesh.

## Time and persistence

### Optional care for a single plant

Care is off by default for all seven species. Manual moisture in `PlantConditions` controls development until the first call to `SetCareEnabled(true)`, which initializes soil from that value. Soil and water doses use a scale from zero to one. Soil only dries when the host calls `Advance`. Each species has a drying rate, stress threshold and maximum droop. The effect is reversible and can continue after the last leaf matures.

```csharp
plant.SetCareEnabled(true);
plant.Advance(6);
float moisture = plant.Care.Soil;
float stress = plant.Care.Stress(plant.Growth.Recipe.species);
plant.Water(0.25f);         // Add a quarter of the pot's capacity.
plant.SetCareEnabled(false); // Resume manual moisture and pause soil drying.
```

Calling `Water()` fills the pot. Re-enabling care resumes the previous soil level. To persist care with the plant, save `JsonUtility.ToJson(PlantSpecimen.Capture(plant))` and load with `JsonUtility.FromJson<PlantSpecimen>(json).Restore(plant)`. Earlier specimens start with care off.

### Jade growth

Set `recipe.species = PlantSpecies.JadePlant`. Each node stores one internode and grows two rounded leaves on opposite sides. The next pair turns roughly a quarter turn around the stem. As a shoot extends, one bud continues from its tip and a second shoot emerges a node lower. Seed changes the orientation and size of the pairs and the fork angles. Spread changes the branch opening, and crown count controls how many shoots start at soil level. A single crown grows up to 35 paired-leaf nodes.

To reproduce **Jade shoot**, use seed 23, one crown, default spread and leaf size, and four starting growth units. Advance twice by six under default conditions to see the first side branch. With optional care enabled, jade soil dries by 0.017 of pot capacity per supplied unit. Leaf stress starts below 22% moisture. Full specimen saves retain both the branch structure and the soil state.

### Spider plantlets and rubber foliage


Spider plant runners extend first, then grow a small rosette at the tip. Once the six juvenile leaves have formed, use the runner's stable node ID to detach it. The daughter initially matches the attached rosette at the soil origin and acquires more leaves and runners as it grows. A saved donor remembers each harvested runner.

```csharp
for (int i = 0; i < mother.Growth.OrganCount; i++)
{
    int id = mother.Growth.NodeAt(i).Id;
    if (!mother.Growth.CanDetachPlantlet(id)) continue;
    var daughter = PlantSpecimen.Capture(mother, mother.DetachPlantlet(id));
    daughter.Restore(nursery);
    break;
}
```

Windowsill copies the mother's care setting and current soil level into the daughter. The two pots then dry and respond to water independently. Call `nursery.Water()` at planting if your game gives a cared-for daughter fresh, watered soil.

Use `PlantletPosition(id)` on the growth record for a selection marker in plant-local coordinates. For rubber plants, set `recipe.species = PlantSpecies.RubberPlant` and set `recipe.variegated` before generating to choose the cream-edged foliage. Crown count controls the number of upright stems.

### Support-aware growth

Set `plant.Supports` before generating or advancing a vine. The default struct leaves the plant free-growing. `PlantSupports.Windowsill` enables a rim and a shelf. Configure the shelf rectangle in local XZ metres, its top height relative to soil, and the rim radius and height. Enable `pole` and set its XZ centre, radius and top height to offer a climbing surface. Sampling these supports at birth makes growth deterministic and allows many specimens to advance without a physics scene. Existing nodes keep their positions and contacts after the layout changes.

### Pruning and propagation

`NodeAt(index)` returns a stable ID, parent ID, shoot ID, mature base and tip, and contact type. Use `CanCut(id)` and `PreviewCut(id)` to select a pothos branch or a mature Monstera node with an aerial root. Monstera nodes become eligible at depth two after five biological growth units. A cut removes the selected node and its descendants and schedules regrowth at the retained parent. The detached Monstera activates a bud at its rooted node. Further cuts before advancing preserve scheduled buds.

```csharp
// mother and nursery are separate ProceduralPlant components in the scene.
var design = PlantSpecimen.Capture(mother, mother.Cut(selectedNodeId, plantUpright: true));
var placement = CuttingPlacement.Climbing;
placement.poleXZ = new Vector2(.045f, .04f);
design.PlantCutting(nursery, placement, PlantSupports.Windowsill);
nursery.Advance(2);
```

Version 1 growth saves and serialized plants acquire stable IDs on load. `SaveState()` captures the living structure and its pending regrowth. `PlantSpecimen.Capture` adds growing conditions, supports, detail and the surface preset. Store project-specific material references alongside a portable specimen when your game uses material overrides.

### Planting and inspecting a vine

Build a live preview by calling `placement.Pose(design, potLayout)` on the same source after each slider change. It returns an independent growth record. Use `placement.SupportsFor(potLayout)` to position the pole model at the same local XZ coordinates as the growth system. The climbing preset turns the cutting as a unit about its base to bring the growing bud beside the pole. Yaw and tilt in degrees add manual adjustments. The trailing preset leaves the planted orientation intact and disables the pole. A cutting whose bud extends beyond the top of the pole can continue freely.

`TryPreviewNext(conditions, supports, out next)` predicts the next stem and contact without changing plant state. `TryPreviewVine(shootId, conditions, supports, out next)` looks ahead to the next birth on a particular shoot, assuming conditions stay constant. Zero follows the next birth on any shoot. Use `VisibleVineTip(next.ParentId)` for the on-screen marker when the parent exists. `next.Base` and `next.Tip` describe the predicted segment at full extension. Each existing node's `Contact` records the support it met when it formed.

### Monstera and variation baking

Later nodes on a Monstera shoot develop larger blades, deeper edge splits, enclosed fenestrations and aerial roots. Openings have front and back edges connected by thin walls at every detail level. The leaf UVs, normals and tangents accompany the baked geometry.

Use `PlantInstances.Living(source, parent)` for an independent runtime plant with the same appearance and growth record. `PlantInstances.Frozen(source, parent)` creates a renderer and owns its copied mesh for the lifetime of that object. Both place the copy at the parent’s local origin. In the editor, `PlantPrefabBaker.Bake(source, assetPath, living)` writes a new prefab under Assets. A static prefab references a persistent mesh asset beside it. The living prefab reconstructs its mesh when enabled. The Windowsill tray uses these workflows to compare and bake individual seeds.

### Development and save data

Starting growth uses the planting conditions. For each supplied time unit, biological age increases by a factor from 0.20 at zero effective moisture to 1.25 at full effective moisture. Existing organs open and elongate according to their birth age. Whole biological ages add an organ until capacity is reached. Structural growth stops six biological units after the last birth, or nine for a spider plant to finish developing its plantlets. An enabled care simulation keeps advancing until the soil dries.

Pothos builds alternate leaves on multiple vines and periodically activates an older node. Snake plants add tapered, folded blades around the base. Ferns add arching fronds whose paired leaflets spread as the frond expands. Crown shoots affect the initial distribution and maximum number of organs. Pothos supports up to 24 organs per crown shoot, capped at 160. Snake plant capacity is 6 + 4 per shoot and fern capacity is 6 + 3 per shoot.

The normalized light direction is expressed in plant-local space. Light response biases the heading of organs as they appear. Switching the direction leaves established attachments and headings intact. Zero direction or response gives the underlying species habit. The preview's studio illumination is constant so you can inspect the actual change of form.

Each pothos shoot has a seeded vigour and a different arch length. New internodes ease out over the crown and descend with a gentle sideways curve. The growth record stores the tangent at each end of a new internode. Neighbouring sections meet along the same tangent, and new leaves emerge at the extending tip. Earlier v1.0 saves keep their original straight internodes and grow curved extensions.

Growth JSON contains the format version, original recipe, biological age and existing organ records. Restore rejects malformed or unsupported growth state with an exception. A full specimen snapshot also stores care, conditions, supports, detail and surface. For offline growth, store your clock timestamp alongside the specimen and advance once per interval in chronological order.

## Meshes and materials

The inspector and sample bake the current plant into a standalone Unity `.asset` at the selected detail level. This includes positions, normals, tangents, UV0, vertex colors, pattern coordinates in UV3, and two submeshes. The first submesh contains stems and petioles and the second contains blades and leaflets. A snake plant uses only the foliage submesh.

Each blade is a closed surface. Its upper and lower faces share the margins and meet at a single vertex at each end. Longitudinal strips follow the midrib and shoulders. Cross sections carry the curve, fold and tapered thickness. Pothos has a basal cleft and two rounded lobes, snake plant has a fleshy twisted channel, and fern leaflets have scalloped edges. Fine lateral veins and pigment markings come from the material.

Set `plant.Detail` or pass a `LeafDetail` value to `PlantMesher.Write`. Changing detail retessellates the same growth record. At **Standard**, one pothos blade has 206 vertices and 408 triangles, a snake plant blade has 254 vertices and 504 triangles, and one fern leaflet has 90 vertices and 176 triangles. **Simple** reduces cross sections for distant views. **Close-up** places more samples around the margins and midrib. In Plant Studio, toggle **Leaf wireframe** to see the actual triangles and current foliage triangle count.

UV0 follows the length of each leaf base-to-tip and spans its width edge-to-edge. Stem tubes have a longitudinal seam. These islands overlap intentionally for tileable textures. There is no unique paint atlas or lightmap unwrap. The supplied **Botanical** material reads vertex colors and UV3 to draw species markings. Custom lit materials can use UV0 directly and may need vertex-color support to reproduce the palette. Assign materials on the component to preview substitutions.

Structural changes rebuild the mesh. On a mature plant, soil drying costs a mesh update only when the visible leaf pose changes. Those updates reuse the topology and UVs. Stagger large groups of wilting plants across frames, or batch elapsed time for unloaded specimens. Use baked meshes for display-only assets. [Shelf profile and architecture](architecture-review.md)

## Using PBR textures

Jade uses a broad waxy highlight on the thick blades. Rubber uses the per-leaf variation to shape its cream margins, and pothos carries muted marbling across the blade. Both included foliage shaders read these markings from the same mesh data. Choose **Selected pot** in Windowsill to inspect a single specimen at its current detail level.

Select **Surface > PBR** on a plant, or set `plant.Surface = PlantSurface.PBR` at runtime. Scene lights illuminate the leaves. The sample has a warm window light and a cool fill. Its simulated growth light is a separate control.

Duplicate **Leaf PBR** into your project's Assets, assign the copy to **Leaf Material**, and edit its texture slots. **Stem Material** accepts a separate material. Explicit overrides take priority over the surface preset. The same materials work on a baked plant.

| Texture | Unity import | Contents |
| --- | --- | --- |
| Base color | Default, sRGB enabled | RGB albedo in leaf UV0. Set **Procedural coloration** to zero to use the texture's own colors, or blend in the generated species markings. |
| Tangent-space normal | Normal map | Detail across the midrib and lateral veins. Adjust **Normal strength** to taste. |
| Occlusion / roughness / metallic | Default, sRGB disabled | Red carries ambient occlusion, green roughness (white is rough), blue metallic. The material has separate multipliers. For an ordinary leaf, leave metallic at zero. |

All three maps use the base color texture's tiling and offset. The supplied foliage set includes a color detail texture, a vein normal map, and a packed surface map. **Long pothos** in Plant Studio uses seed 63, five crown shoots, leaf size 0.91 and 90 starting growth units.
