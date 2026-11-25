# Texture Atlas

Open a scene in **Scenes** to compare its swatches in the Game view. The gallery works in Edit mode. Each swatch uses an imported texture from **Recipes**. Select a `.proctex` file, change a setting in its Inspector and choose **Apply** to see the material update. **Apply as Copy** makes a variation you can keep. The optional jlinkdev Sample Framework supplies the corner title.

| Scene | What to try |
| --- | --- |
| Resolution | Compare **Folded terrain 64** with **Folded terrain 512**. Both use seed 53 and identical ridged-noise settings. The lower row enlarges the center 16 × 16 and 128 × 128 texels from the same UV region. Change the smaller recipe to 512 × 512 and choose **Apply** to match the higher-resolution swatch. The scene labels describe the original recipes. |
| Processing | Compare **Stripes** with **Warped stripes**. Change the warp seed while keeping the stripe settings. **Warped outlines** applies blur and edge detection to that warp, and **Repeated outlines** displays the same result across four tiles. The second row compares a circle with isotropic blur, directional blur and its Sobel outline. |
| Gradients and LUTs | Select **Thermal LUT** and move the orange stop in its gradient. Both the 256 × 1 strip and **LUT on fractal noise** update. **Banded LUT** uses the same stops with fixed interpolation. |
| Noise | Compare **Value noise**, **Perlin** and **Simplex** at seed 17 and five cells per axis. **Fractal noise** and **Fractal threshold** start from the same five-octave field. Change the threshold to expand or shrink the bright region. |
| Masks | Compare **Star** with **Star distance field**. A value of 0.5 marks the distance-field boundary. Increase **Distance range** to widen the transition. **Particle alpha** puts a radial falloff in opacity, visible against the swatch checkerboard. |
| Patterns | Compare regular **Diamond stamps** with **Scattered stamps**. Change the scattered seed or jitter and inspect **Repeat across seams**, which draws the same texture across four tiles. |

## Using the LUT in a shader

The two LUTs use a full-width linear gradient, zero rotation and offset, and **Fit to texel endpoints**. Their first and last texels store the two ends of the ramp. The sample's **Display Field** shader reads the red channel of **Fractal noise** and uses that value to address the strip. With bilinear filtering, the sample coordinate for a normalized scalar `t` is `u = (0.5 + t * (width - 1)) / width`, `v = 0.5`. The gradient swatches stretch their one-row textures vertically for viewing.
