# Interactive Fog

Bounded, interactive ground mist for Unity 6 URP. A shallow 3D GPU fluid, or an optional 2D column solver, transports fog; volumetric rendering gives it depth, self-shadowing, and local light. Full, softly tapered billows maintain connected coverage, with sparse irregular local curls and broad horizontal circulation. Moving capsules entrain the fog and leave persistent wakes. Gusts can be driven by attacks, landings, or environmental machinery.

## Requirements

- Unity 6000.0 or later, URP 17.0.3 or later, **Render Graph enabled**.
- Compute shaders, 3D floating-point UAV textures, a 2D RGHalf UAV texture, and a depth texture. Desktop GPU baseline; XR is not supported.
- Linear color space is recommended. See the [validation record](Documentation~/Validation.md) for actual tested configurations and performance.

## Installation

Add this folder as a local package through **Window → Package Manager → + → Add package from disk**, selecting `package.json`. The package declares its URP and Unity module dependencies. For a repository revision containing this package, the UPM Git URL is `https://github.com/jlinkdev/unity-utilities.git?path=/Packages/com.jlinkdev.interactive-fog`; pin a known revision for reproducible installs.

## Quick start

1. Open **Tools > jlinkdev > Interactive Fog > Setup**. Choose your pipeline and camera; the window resolves its renderer override. Confirm the Universal Renderer Data and click **Configure Selected Renderer**. This adds or repairs the fog feature and assigns its shader; other renderer features are retained.
2. Enable the selected pipeline's depth texture and ensure **Render Graph** is enabled. Cameras using a renderer override need the feature on that renderer too.
3. Create **Ground Mist**, **Cumulus** or **Dry Ice** from the setup window, or **GameObject > Effects > Interactive Fog**. The new volume has its compute shader and matching shared profile assigned.
4. Position the volume at floor height and set **Size (m)**. Bounds are world-aligned; rotation and scale are ignored. Assign **Sun** and optional point lights under **Lighting**.
5. Add **Fog Interactor** to a moving character at its feet. Leave **Clearing** at zero for displacement without density deletion. Add **Fog Obstacle** to solid architecture and fit its box to the geometry.
6. On the volume, click **Select Style Asset** to tune its appearance and motion. **Save Current Look as Style Profile...** creates and assigns an independent copy; **Duplicate Profile...** creates an unassigned copy. Quality stays on the volume. Use **Reset Simulation** for a fresh comparison.
7. Start performance tuning with **Render Resolution > Half** under **Simulation and Rendering Quality**. **Planar2D** reduces fluid work and memory with approximate vertical behavior. See the [performance guide](Documentation~/Performance-and-Styles.md).

| Profile | Main controls |
| --- | --- |
| Sanctuary Ground Mist | Fog Height, Density, Fold Size, Rest Recovery |
| Cumulus | Puff Size, Cloud Coverage, Fog Height, Density; optional Wisp Opacity |
| Dry Ice | Billows: Spacing, Coverage, Radius, Growth/Settling/Quiet intervals; separate body Density and Mist Amount |

```csharp
// Call as the blade moves. The wake continues after the force expires.
fog.AddSweep(previousBladeTip, currentBladeTip, bladeVelocity,
             radius: .3f, duration: .06f);
```

For procedural dry-ice billows, assign `Runtime/Styles/Dry Ice.asset`. No emitter is needed. Click **Select Style Asset** on the volume and edit **Billows** for spacing, coverage, radius and growth/settling/quiet-time ranges. The field releases mist into the selected fluid solver and retains interaction and recovery. See the [style guide](Documentation~/Performance-and-Styles.md#dry-ice-billows).

## Sample

Import **Mist Sanctuary** from the package's Samples tab and open its scene. Use **Tools > jlinkdev > Interactive Fog > Setup** to register the imported **Mist Renderer** in your active pipeline and assign it to **Sanctuary camera**; save the scene. Press Play: the character follows a closed animation path with animated feet and a timed blade sweep. There is no input controller or input-backend requirement. The fixed camera and finer sample grid expose curls, wisps, and residual wakes. The sample uses a camera renderer override and leaves project pipeline references and the default renderer unchanged. See the [sample guide](<Samples~/Mist Sanctuary/README.md>).

## Limitations

- This is a visual approximation of low fog, not a physically calibrated fluid.
- Dry Ice combines authored billow lifetimes with simulated released mist. It does not simulate bubble films, heat, phase changes or spilling over terrain edges.
- Use one volume per space. Separately composited, overlapping volumes do not produce correct combined scattering.
- Planar2D reconstructs vertical shape and cannot independently simulate vertical rolls or height-selective obstacle response. Half/Quarter rendering can soften fine detail.
- Finite grid resolution limits small eddies and thin walls. Box boundaries are voxelized; fast motion can cross thin solids.
- Directional self-shadowing has a finite march distance. Point lighting uses a short approximate shadow march; spot/area lights are not supported.
- Transparent objects render after the fog and do not receive integrated fog. XR and compatibility-mode rendering are unsupported.
- Up to 16 combined interactors/gusts and 16 obstacle boxes per volume. Oversubscription warns and ignores later entries; keep nearby registrations within these limits.
- The solver advances at a fixed rate with at most three substeps per frame; it deliberately drops excess time during severe hitches.

## Documentation

- [Setup and troubleshooting](Documentation~/Setup-and-Troubleshooting.md)
- [Architecture and integration](Documentation~/Architecture.md)
- [Tested configurations and validation](Documentation~/Validation.md)
- [Performance controls and style profiles](Documentation~/Performance-and-Styles.md)
- [Changelog](CHANGELOG.md)

## License

[MIT](LICENSE.md). The code and sample geometry were authored for this package.
