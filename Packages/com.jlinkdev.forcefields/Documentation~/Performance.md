# Performance guide

Forcefield cost is primarily determined by screen coverage, impact capacity, and quality.

## Recommended order of optimization

1. Reduce impact capacity from 32 or 16 to 8.
2. Use a Medium or Low preset for distant or numerous fields.
3. Disable chromatic split, which adds two scene-color samples.
4. Disable intersection glow when depth interaction is not visible.
5. Reduce the number and screen size of overlapping transparent shells.

The runtime reuses one `MaterialPropertyBlock` and fixed arrays. It uploads state only when an impact, preset, intensity, renderer transform, or blend changes. Normal animated motion uses shader time and has no per-frame managed allocation.

Each forcefield is still a transparent draw call with unique per-renderer data. Profile representative target hardware instead of assuming that a desktop preset is suitable for mobile.
