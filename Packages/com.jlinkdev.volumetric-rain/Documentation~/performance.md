# Performance investigation — 2026-09-19

## Result

At 1920×1080 on an NVIDIA GeForce RTX 2060 SUPER (D3D11), the final default
rain pass measured **5.77 ms median**, versus **10.98 ms** for the original shader:
about **47% less GPU time**. All six original benchmark scenarios produced
byte-identical PNG captures before and after, including the final volume-capable
renderer when volumes were disabled. Resolution, two-grid coverage, capsule
filtering, field distribution, and all visual defaults were preserved.

These are exploratory editor GPU measurements, not a promised player frame rate.
The host editor and other desktop applications remained open. The original and final shaders were measured back-to-back in the same process.
Earlier 120-frame runs measured roughly 5.7–6.4 ms for optimized default rain.
A run overlapping a separate player smoke test was discarded because of GPU
contention; that player was stopped before this paired measurement. A matched GPU particle/VFX comparison is still
needed to answer the prototype's original relative-performance question.

## What was expensive

The original traversal evaluated eight-corner density noise for every occupied
candidate capsule, even when its computed pixel coverage was zero. Most capsules
miss the pixel, so much of that hashing and interpolation could not affect the
image. Disabling density noise reduced the original pass from 10.98 to 5.57 ms,
which isolated a substantial avoidable cost.

The shader now checks coverage and distance fade before evaluating density noise
and brightness. Haze also skips work when extinction is zero and avoids noise
samples whose transition is zero. These optimizations are always enabled because
they remove zero-contribution work, without lowering quality.

Smaller cells remain more expensive: a ray crosses more cells, each generating
and testing candidates. Denser settings also execute more capsule work despite
fixed traversal limits. This renderer has no particle-count ceiling, but density
is not computationally free.

## Measurements

Unity 6000.0.58f1, URP 17.0.3, Windows, D3D11, RTX 2060 SUPER. Full-resolution
1920×1080 render target, MSAA off, default profile, frozen time = 2, a fixed camera
and four greybox occluders. Each case warms up for 120 frames and records 300 frames.
The Unity `Volumetric Rain` GPU profiling scope is measured separately from CPU
submission and readback. A one-pixel readback fences completion; full PNG capture
happens outside the timed region. The fixture restores Graphics/Quality settings.

| Scenario | Original GPU median | Final GPU median | Final GPU p95 |
| --- | ---: | ---: | ---: |
| Default dense rain | 10.98 ms | 5.77 ms | 6.63 ms |
| Density noise disabled | 5.57 ms | 5.43 ms | 6.24 ms |
| Cell size 1.3 m instead of 0.65 m | 5.58 ms | 3.12 ms | 3.81 ms |
| Density 0.2 instead of 0.8 | 4.84 ms | 3.29 ms | 4.02 ms |
| Haze extinction zero | 11.15 ms | 5.46 ms | 6.29 ms |

The largest gain is with the default density noise enabled. Noise-disabled gains
are small; timing varies with GPU scheduling and clocks, and the new coverage
branch is not free. The unoptimized shader remains in the development benchmark
for follow-up comparisons.

With intensity zero, there is no rain GPU scope. The final synchronized whole
render/readback median was 2.00 ms in that case; this is not the rain pass cost.

| Final volume scenario | GPU median | GPU p95 |
| --- | ---: | ---: |
| One bounded 30×20×40 m rain box | 3.71 ms | 4.55 ms |
| Global rain with a 10×6×10 m dry box around camera | 4.90 ms | 5.85 ms |
| Every ray entirely inside a dry box | 0.14 ms | 0.14 ms |

Volume cases intentionally change the domain, so their images are not comparisons
of equal rain coverage. They demonstrate that skipping dry intervals can repay
the box-intersection cost. A fully dry view still composites/copies camera color;
bounded mode with no visible rain boxes skips the rain pass entirely.

## Existing quality/performance choices

No new quality-reducing default was introduced. The existing controls remain
explicit authoring choices:

- Larger cells reduce candidate count and change the rain's spatial frequency.
- Smaller Max Cell Steps shortens the effective streak range with a smooth fade.
- Lower Haze Steps reduces integration accuracy, especially with density noise.
- Zero Noise Strength removes large-scale density structure.
- Lower resolution/render scale reduces pixel work but can soften/alias streaks.

Half-resolution rendering and temporal reconstruction were not added: fine rain
and silhouette quality would need a separate measured design and opt-in controls.

## Reproduce

The development-only fixture is
`Assets/PackageDevelopment/VolumetricRain/Benchmark/RainBenchmark.cs`. Its Reference
folder contains the original shader. It is deliberately outside the UPM package.
Run it in an isolated project with the package, Unity Test Framework, and that
Benchmark folder copied into Assets. Add the package to manifest `testables`.
The fixture creates an empty scene; do not run it with unsaved scene work.

In PowerShell, set `RAIN_BENCH_VARIANT` to `Compare` (back-to-back), `Reference`, or `Final`, and set
`RAIN_BENCH_OUTPUT` to an absolute output directory. Then run:

```text
unity test <isolated-project> --mode EditMode --filter RainBenchmark.Measure --output <results.xml>
```

CSV, hardware metadata, and captures are written to that directory. The repository
run artifacts are in `Logs/VolumetricRain/Performance`. Small CSV records and
capture hashes accompany this document under `performance-data`; images and logs
are not included in the distributed package.

## Fog-only follow-up — 0.3.0, 2026-09-25

Measured using the same Unity 6000.0.58f1 / URP 17.0.3 / D3D11 / RTX 2060 SUPER
fixture at 1920×1080, 120 warm-up frames and 300 measured frames per case:

| Mode/domain | GPU median | GPU p95 |
| --- | ---: | ---: |
| Default Rain And Fog | 5.75 ms | 7.47 ms |
| Fog Only, unbounded | 0.76 ms | 1.49 ms |
| Fog Only, bounded 30×20×40 m box | 0.63 ms | 1.15 ms |

Fog Only uses density 0.8, extinction 0.015, noise strength 0.35 and 12 fog samples.
It fills near regions too, so these are different rendered effects, not equal-image
quality comparisons. It compiles out streak traversal. Timings are exploratory
editor measurements and will vary by GPU, resolution, box coverage and noise cost.

All six original unbounded scenario captures are byte-identical to the recorded
pre-mode implementation. Data is in `performance-data/Fog030.csv`; captures and
raw output are under `Logs/VolumetricRain/Performance/Fog030-*`.