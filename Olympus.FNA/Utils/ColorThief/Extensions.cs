using Microsoft.Xna.Framework;
using System;

namespace Olympus.ColorThief {
    public static class Extensions {

        public static HslColor ToHsl(this Color c) {
            const double toDouble = 1.0 / 255;
            double r = toDouble * c.R;
            double g = toDouble * c.G;
            double b = toDouble * c.B;
            double max = Math.Max(Math.Max(r, g), b);
            double min = Math.Min(Math.Min(r, g), b);
            double chroma = max - min;
            double h1;

            // ReSharper disable CompareOfFloatsByEqualityOperator
            if (chroma == 0) {
                h1 = 0;
            } else if (max == r) {
                h1 = (g - b) / chroma % 6;
            } else if (max == g) {
                h1 = 2 + (b - r) / chroma;
            } else /*if (max == b)*/ {
                h1 = 4 + (r - g) / chroma;
            }

            double lightness = 0.5 * (max - min);
            double saturation = chroma == 0 ? 0 : chroma / (1 - Math.Abs(2 * lightness - 1));
            HslColor ret;
            ret.H = 60 * h1;
            ret.S = saturation;
            ret.L = lightness;
            ret.A = toDouble * c.A;
            return ret;
            // ReSharper restore CompareOfFloatsByEqualityOperator
        }

    }
}
