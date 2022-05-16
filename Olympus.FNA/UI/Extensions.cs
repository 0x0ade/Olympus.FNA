using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlympUI {
    public static class Extensions {

        public static string? NotEmpty(this string? s)
            => string.IsNullOrEmpty(s) ? null : s;

        public static Color Multiply(this Color a, Color b)
            => new(
                (byte) ((a.R * b.R) / 255),
                (byte) ((a.G * b.G) / 255),
                (byte) ((a.B * b.B) / 255),
                (byte) ((a.A * b.A) / 255)
            );

        public static Vector2 Round(this Vector2 xy)
            => new(MathF.Round(xy.X), MathF.Round(xy.Y));

        public static Point ToPoint(this Vector2 xy)
            => new((int) xy.X, (int) xy.Y);

        public static Point ToPointRound(this Vector2 xy)
            => new((int) MathF.Round(xy.X), (int) MathF.Round(xy.Y));

        public static Point ToPointCeil(this Vector2 xy)
            => new((int) MathF.Ceiling(xy.X), (int) MathF.Ceiling(xy.Y));

        public static Vector2 ToVector2(this Point xy)
            => new(xy.X, xy.Y);

        public static Point ToPoint(this MouseState xy)
            => new(xy.X, xy.Y);

    }

    public class CompilerSatisfactionException : Exception {

        public CompilerSatisfactionException()
            : base("This member merely exists to satisfy the compiler. Sorry!") {
        }

    }

    public interface IGenericValueSource {

        T GetValue<T>();

    }
}
