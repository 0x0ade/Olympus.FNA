using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OlympUI;
using System.Collections.Generic;
using System.Linq;

namespace Olympus.ColorThief {
    public static class ColorThief {

        public const int DefaultColorCount = 5;
        public const int DefaultQuality = 10;

        /// <summary>
        ///     Use the median cut algorithm to cluster similar colors and return the base color from the largest cluster.
        /// </summary>
        /// <param name="sourceImage">The source image.</param>
        /// <param name="quality">
        ///     1 is the highest quality settings. 10 is the default. There is
        ///     a trade-off between quality and speed. The bigger the number,
        ///     the faster a color will be returned but the greater the
        ///     likelihood that it will not be the visually most dominant color.
        /// </param>
        /// <param name="ignoreWhite">if set to <c>true</c> [ignore white].</param>
        /// <returns></returns>
        public static QuantizedColor GetDominantColor(this IReloadable<Texture2D, Texture2DMeta> sourceImage, int quality = DefaultQuality) {
            List<QuantizedColor> palette = GetPalette(sourceImage, 3, quality);

            QuantizedColor dominantColor = new(
                new() {
                    A = (byte) palette.Average(a => a.Color.A),
                    R = (byte) palette.Average(a => a.Color.R),
                    G = (byte) palette.Average(a => a.Color.G),
                    B = (byte) palette.Average(a => a.Color.B)
                },
                (int) palette.Average(a => a.Population)
            );

            return dominantColor;
        }

        /// <summary>
        ///     Use the median cut algorithm to cluster similar colors.
        /// </summary>
        /// <param name="sourceImage">The source image.</param>
        /// <param name="colorCount">The color count.</param>
        /// <param name="quality">
        ///     1 is the highest quality settings. 10 is the default. There is
        ///     a trade-off between quality and speed. The bigger the number,
        ///     the faster a color will be returned but the greater the
        ///     likelihood that it will not be the visually most dominant color.
        /// </param>
        /// <param name="ignoreWhite">if set to <c>true</c> [ignore white].</param>
        /// <returns></returns>
        /// <code>true</code>
        public static List<QuantizedColor> GetPalette(this IReloadable<Texture2D, Texture2DMeta> sourceImage, int colorCount = DefaultColorCount, int quality = DefaultQuality) {
            Color[] pixels = sourceImage.GetData();
            return Mmcq.Quantize(pixels, colorCount, quality)?.GeneratePalette() ?? new List<QuantizedColor>();
        }

    }
}
