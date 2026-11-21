# Rendering and tuning

Add the feature once per Universal Renderer that serves a blueprint camera. Each enabled Blueprint Camera on a base camera has its own palette, inclusion mask and layer budget. An overlay camera can draw a HUD after the blueprint. The pass runs before post-processing, so a camera's color grading can affect its ink and paper.

## Geometry capture

The first pass renders both sides of eligible surfaces into a cleared floating-point target. RGB holds the world-space mesh normal and alpha holds positive eye depth. A private hardware depth buffer selects the closest fragment. Each subsequent pass discards fragments at or in front of the prior depth and captures the nearest remaining surface. A back wall and a front wall of the same solid can therefore occupy two layers. The camera's near and far planes and ordinary renderer culling still apply.

The final pass uses normal changes, depth discontinuities, and the edge of each layer's coverage to find lines. Where several lines fall on the same pixel, the strongest ink wins. Hidden ink controls deeper layers together. A zero layer mask yields a blank sheet. This method runs without mesh preprocessing and accepts moving geometry.

## Quality and cost

Choose the smallest layer count that reveals the desired features. Each layer adds a draw of the selected geometry, a full-resolution RGBA32F surface (about 32 MiB at 1920 × 1080), and four neighboring samples per covered pixel during composition. Render Graph can reuse the temporary depth-test target between captures. Cull unrelated layers and use a smaller RenderTexture for an inset drawing.

Increase crease angle to reduce fine normal changes on curved parts. Subpixel features can flicker during motion. Wider lines improve small preview readability. Exact coincident surfaces share a depth and thin surfaces within the numerical peeling tolerance can merge. Pixel contours have no persistent path along which to place drafting dashes.

The approach follows the blueprint portion of the original chapter. [NVIDIA GPU Gems 2, chapter 15](https://developer.nvidia.com/gpugems/gpugems2/part-ii-shading-lighting-and-shadows/chapter-15-blueprint-rendering-and-sketchy)
