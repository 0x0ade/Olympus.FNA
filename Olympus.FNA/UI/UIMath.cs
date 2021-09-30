using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OlympUI {
    public static class UIMath {

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Intersect(
            Vector2 v11, Vector2 v12,
            Vector2 v21, Vector2 v22
        ) {
            // Based on https://stackoverflow.com/questions/4543506/algorithm-for-intersection-of-2-lines
            // and https://www.topcoder.com/thrive/articles/Geometry%20Concepts%20part%202:%20%20Line%20Intersection%20and%20its%20Applications

            float a1 = v12.Y - v11.Y;
            float b1 = v11.X - v12.X;
            float c1 = a1 * v11.X + b1 * v11.Y;

            float a2 = v22.Y - v21.Y;
            float b2 = v21.X - v22.X;
            float c2 = a2 * v21.X + b2 * v21.Y;

            float delta = a1 * b2 - a2 * b1;
            return new(
                (b2 * c1 - b1 * c2) / delta,
                (a1 * c2 - a2 * c1) / delta
            );
        }

        public static Color ParseHexColor(string text) {
            if (!TryParseHexColor(text, out Color c))
                throw new InvalidOperationException("Cannot parse hex color from given string");
            return c;
        }

        public static bool TryParseHexColor(string text, out Color c) {
            c = new(0, 0, 0, 255);

            if (text.Length == 0)
                return false;

            if (text[0] == '#')
                text = text[1..];

            if (!uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint raw))
                return false;

            if (text.Length > 6) {
                c.R = (byte) ((raw >> 24) & 0xFF);
                c.G = (byte) ((raw >> 16) & 0xFF);
                c.B = (byte) ((raw >> 8) & 0xFF);
                c.A = (byte) ((raw >> 0) & 0xFF);
            } else {
                c.R = (byte) ((raw >> 16) & 0xFF);
                c.G = (byte) ((raw >> 8) & 0xFF);
                c.B = (byte) ((raw >> 0) & 0xFF);
            }
            return true;
        }

        public static string ToHexString(this Color c) =>
            c.A == 255 ?
            $"#{c.R:X2}{c.G:X2}{c.B:X2}" :
            $"#{c.R:X2}{c.G:X2}{c.B:X2}{c.A:X2}";

    }
}
