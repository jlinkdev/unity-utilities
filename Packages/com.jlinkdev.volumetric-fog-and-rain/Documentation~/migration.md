# Package migration

## Interactive Fog consolidation

Install `com.jlinkdev.volumetric-fog-and-rain` for both atmospheric weather and interactive ground fog. If a project previously installed `com.jlinkdev.interactive-fog`, replace that package entry with the merged package in the same dependency change. Both versions contain the same ground-fog assemblies and asset GUIDs.

Existing ground-fog components, profiles and shader references retain their serialized identities. Code referencing the `jlinkdev.InteractiveFog` assembly can keep that reference. For asset lookups built from path strings, the ground-fog shaders and styles now live under `Packages/com.jlinkdev.volumetric-fog-and-rain/Runtime/InteractiveFog/`. Import **Mist Sanctuary** from the merged package's Samples tab and use **Tools > jlinkdev > Volumetric Fog and Rain > Interactive Fog Setup**.

## Volumetric Rain to Volumetric Fog and Rain 0.4

The package is now named **jlinkdev Volumetric Fog and Rain** throughout Package
Manager, its Git path, namespace, assemblies, and editor menus.

| Item | Previous | Current |
| --- | --- | --- |
| Package ID / folder | `com.jlinkdev.volumetric-rain` | `com.jlinkdev.volumetric-fog-and-rain` |
| Namespace | `jlinkdev.UnityUtilities.VolumetricRain` | `jlinkdev.UnityUtilities.VolumetricFogAndRain` |
| Runtime assembly | `jlinkdev.VolumetricRain` | `jlinkdev.VolumetricFogAndRain` |
| Editor menu group | `jlinkdev > Volumetric Rain` | `jlinkdev > Volumetric Fog and Rain` |
| Shader | `Hidden/jlinkdev/Volumetric Rain` | `Hidden/jlinkdev/Volumetric Fog and Rain` |

Install using this Git URL once the changes are published:

```text
https://github.com/jlinkdev/unity-utilities.git?path=/Packages/com.jlinkdev.volumetric-fog-and-rain
```

Replace the old dependency rather than installing both identities together. Update
`using` directives, fully qualified type names, custom `.asmdef` references, and
any hard-coded package paths or shader lookups. The Editor, EditorTests, and
PlayModeTests assembly names have the same `VolumetricFogAndRain` stem. Testable
package entries must use the new package ID too.

Component names such as `RainProfile`, `RainVolume`, and `VolumetricRainCamera`
remain unchanged. Asset/script GUIDs are preserved, and Unity object types carry
`MovedFrom` metadata for their former namespace/assembly. Existing scene and
profile references are intended to survive the move; ordinary C# source references
still need the namespace update. Host authoring assets now live under
`Assets/PackageSamples/com.jlinkdev.volumetric-fog-and-rain`.

Defaults preserve the previous renderer: before post-processing, hard boxes,
rain-linked combined fog, zero height falloff, stationary noise, no directional
lighting, and original scene-alpha handling. Existing Fog Only profiles still use
`Max Distance`. The updated Fog Laboratory intentionally enables the new soft
edges, ground-height falloff, drifting noise, and directional glow.
