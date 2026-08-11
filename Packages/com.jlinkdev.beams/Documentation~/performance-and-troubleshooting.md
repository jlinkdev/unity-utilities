# Performance and troubleshooting

## Cost controls

- CPU cost scales with final strand segment count, not visible shader detail.
- Prefer GPU vertex/fragment noise for rapid fine detail.
- Use CPU electrical or structural noise only when contacts or branch attachment
  need to follow the displaced silhouette.
- Reduce electrical subdivisions before reducing shader quality.
- Set `BeamPhysicsContacts.Query Interval` above zero when contacts do not need
  frame-level response.
- Size `Maximum Hits Per Segment` for the expected density. A full buffer means
  additional hits on that segment are intentionally ignored.
- Branching multiplies both mesh and contact work. Branch width falloff is visual;
  it does not remove physics queries.
- Increase ribbon bounds padding to cover shader displacement or the GPU-deformed
  beam may be culled near camera edges.

Runtime buffers, dictionaries, meshes, and renderer lists are reused. UnityEvent
listeners and user callbacks can still allocate depending on project code.

## The beam is invisible

- Assign a material using `jlinkdev/Beams/Energy Beam`.
- Confirm source and target are active and a path provider/renderer is assigned.
- Ensure the camera is included in the material's URP renderer and transparent queue.
- For custom shaders, follow the mesh channel contract exactly.

## Contacts do not match shader movement

This is expected when GPU vertex displacement is enabled. Physics follows the
final CPU strand. Add a CPU noise/electrical modifier for broad motion, increase
the contact radius to cover purely visual movement, or disable shader displacement.

## Endpoints detach visually

Use the shader endpoint falloff and keep CPU modifiers endpoint-pinned. Large
material displacement requires enough ribbon bounds padding.

## Lightning looks too smooth

Use `BeamElectricalModifier` for angular structural geometry, increase detail
layers, use `Snap` or `Hold And Morph`, and pair it with the Electrical Arc material.
