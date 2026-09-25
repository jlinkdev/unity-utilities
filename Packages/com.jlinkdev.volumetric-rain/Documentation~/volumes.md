# Rain/fog volumes and clear interiors

The renderer still uses a fullscreen pass, but its integration domain can now be
bounded in world space. The fullscreen triangle is only how rays are dispatched;
it does not require rain to fill the entire view or surround the camera.

## Authoring

- **Rain Profile > Extent > Unbounded** retains the original global rain field.
- **Volumes Only** renders inside the union of active **Rain Volume** boxes. With
  no visible rain box, the pass is skipped.
- **Rain Exclusion Volume** subtracts a box from either mode. It removes both
  streaks and this package's haze, including when the camera is inside the box.

Add these components to empty GameObjects using Add Component > jlinkdev >
Volumetric Rain. Adjust Center and Size in local units, or resize with Scene view
handles. Transform position, rotation, parenting, and nonuniform/negative scale
are supported. Zero-scale transforms are ignored. No Collider is required.

Rain boxes have cyan gizmos; exclusions have orange gizmos. Disabled components
and inactive GameObjects are ignored. The camera's culling mask filters the
GameObject layers. Boxes can be moved and resized at runtime without simulating
or respawning drops. Moving the box reveals another part of the same world field;
it does not carry its own population of drops.

For a building, fit exclusions to the rooms or covered spaces. From inside, rays
start dry and can enter rain beyond an open doorway or window. Scene depth still
stops those rays at opaque walls. Roof geometry does not automatically create an
exclusion: this is authored shelter, not a rainfall shadow map. Transparent window
glass remains subject to the usual depth-writing limitation.

## Limits and behavior

- Up to **4 rain boxes and 8 exclusion boxes visible to each camera**. Off-frustum
  and layer-culled boxes do not count. Exceeding the limit skips rain for that
  camera and logs a warning once per renderer feature lifetime; it never silently
  ignores a dry box. Unbounded mode ignores rain boxes entirely.
- All boxes use the camera's shared profile. Overlapping rain boxes are unioned,
  so they do not brighten rain twice. Exclusions always win.
- Exclusions include a 1 mm world-space tolerance to seal numerical gaps against
  flush walls and floors. It is independent of transform scale and baked into
  the uploaded matrix, so it adds no per-pixel padding calculations.
- Boundaries are hard spatial cuts; feathering and arbitrary mesh/SDF volumes are
  not implemented. Capsules have approximate filtered coverage at the boundary.
- Boxes remove only this rain system's haze, not another fog renderer's output.

## Implementation and performance

Per camera, active boxes are frustum/layer culled and inverse transforms are
snapshotted into Render Graph pass data. Each pixel intersects its depth-clipped
ray with unit boxes in local coordinates. Wet intervals are sorted and unioned;
sorted dry intervals are subtracted. Four wet boxes minus eight dry boxes produce
at most twelve intervals. Only those intervals enter capsule traversal.

The existing per-grid cell budget is shared across all intervals. Two extra visits
are conservatively reserved per additional interval when calculating the smooth
distance fade. Fragmenting the view heavily can therefore shorten the effective
streak range at small budgets; it cannot create unlimited traversal work.

In Rain And Fog mode, haze integrates each wet interval after the near/mid transition.
Fog Only integrates the whole interval independently of rain distances; Rain Only
omits integration. See [fog modes](fog.md). Samples are
allocated by wet length, with at least one per contributing interval. This keeps
thin rain regions from disappearing between samples. The actual count can be up
to Haze Steps + 11. Haze sample positions differ from unbounded integration because
the domain has changed. There is no temporal history or screen-space jitter.

Box intersection has a cost, but skipping dry distance or pixels can more than
offset it. Global rain without visible exclusions uses a separate shader variant
with the entire interval-processing path compiled out.

## Demo

Import **Volume Laboratory** from Package Manager's Samples tab, or run **Tools >
jlinkdev > Volumetric Rain > Create Volume Laboratory**. Open the scene alone and
press Play. Its camera starts in a dry canopy facing outdoor rain. Select the
orange dry volume and disable it to see the difference. Move the camera through
the open front, or resize the cyan rain volume to inspect its outer boundary.
The original Rain Laboratory remains available unchanged.
