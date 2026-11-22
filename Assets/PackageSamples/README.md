# Editable package samples

The editable scenes live under `Assets/PackageSamples/<package-id>/<sample>/Scenes`.
Open a scene there, edit it, and save. Select its folder or any asset inside it and
choose **Assets > jlinkdev > Publish Package Sample**. The publisher updates the
registered `Samples~/<sample>` folder, including additions, renames, and removals.
Imported demos under `Assets/Samples` can coexist with these editable copies.

Use Python 3.9 or newer on PATH for publishing. From the repository root:

```sh
python scripts/package_samples.py --publish com.jlinkdev.beams "Orbital Descent"
python scripts/validate-packages.py
```

Omit the package and sample arguments to publish all registered samples. Run
`python scripts/package_samples.py` for a read-only comparison. Each local asset
has a distinct, stable GUID in each copy. Publish after moving an asset with its
`.meta` file to carry its identity through the rename. The publisher remaps local
references and the editable script/assembly names. References to package runtime
assets retain their original IDs.

Keep `README.md` at the sample root and scenes in `Scenes/`. Add populated folders
such as `Scripts/`, `Settings/`, `Profiles/`, `Materials/`, and `Prefabs/` as needed.
When adding sample scripts, use a namespace containing `.EditableSamples` in the
editable copy. Prefix sample assembly names with `EditableAssembly.` when using
an asmdef. The publisher translates these names for the imported copy.

## Authoring scenes

Save the camera, lights, environment, interactive objects, and their references in the scene or its prefab instances. Opening the scene in Edit mode should expose the objects someone would want to move, inspect, or replace. Generate meshes, effects, and spawned objects at runtime when generation or spawning is part of the demonstration.

Use the host project's URP pipeline. For sample-specific rendering, serialize the feature reference on the sample camera and request its required textures there. Preview camera effects in the Game view. Renderer-list slots belong to a particular pipeline and cannot travel with an imported scene. Sample import, scene builders, and Play mode must preserve the host's Graphics, Quality, Player settings, and renderer assets.

The independent URP packages ship their camera pass components from a shared source template. After editing `scripts/camera_pass.cs.template`, run `python scripts/camera_passes.py`. Package validation checks that every component matches the template.

See the [development tools guide](../PackageDevelopment/README.md) for scene builders and benchmarks.

## Shared sample presentation

Each sample has an editable **UI/Sample Identity.uxml** containing its package name, sample name, and icon. The scene's **Sample Identity** UI Document references the framework panel. Shared Barlow fonts, outlined icons, controls, captions, and grid surfaces live in **Runtime/Presentation** in the framework package. Keep demo-specific models, materials, controls, and scenes in their individual samples.

The gallery and pottery workbench use a studio floor. Mist Sanctuary uses the tile texture on its masonry. Choose a stage that suits the demonstration. The nebula studies retain their space environment.

After adding or renaming a registered sample, publish the pair and run **python scripts/sample_installers.py**. It updates the Editor-only helper and its published README markers for each utility. [Installer and release workflow](../../Packages/com.jlinkdev.object-highlighting/Documentation~/sample-framework.md)
