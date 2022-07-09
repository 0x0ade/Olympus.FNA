using Microsoft.Xna.Framework;
using System;

namespace OlympUI {
    public unsafe struct Padding {

        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int L {
            get => Left;
            set => Left = value;
        }

        public int T {
            get => Top;
            set => Top = value;
        }

        public int R {
            get => Right;
            set => Right = value;
        }

        public int B {
            get => Bottom;
            set => Bottom = value;
        }

        public int W => Left + Right;
        public int H => Top + Bottom;

        public Point LT => new(Left, Top);
        public Point RB => new(Right, Bottom);
        public Point WH => new(W, H);

        public int this[int side] {
            get => side switch {
                0 => Left,
                1 => Top,
                2 => Right,
                3 => Bottom,
                _ => throw new ArgumentOutOfRangeException(nameof(side))
            };
            set {
                fixed (Padding* self = &this) {
                    *(side switch {
                        0 => &self->Left,
                        1 => &self->Top,
                        2 => &self->Right,
                        3 => &self->Bottom,
                        _ => throw new ArgumentOutOfRangeException(nameof(side))
                    }) = value;
                }
            }
        }

        public int Max => Math.Max(Math.Max(Left, Right), Math.Max(Top, Bottom));

        public Padding(int ltrb) {
            Left = Top = Right = Bottom = ltrb;
        }

        public Padding(int lr, int tb) {
            Left = Right = lr;
            Top = Bottom = tb;
        }

        public Padding(int l, int t, int r, int b) {
            Left = l;
            Top = t;
            Right = r;
            Bottom = b;
        }

        public static implicit operator int(Padding p) => p.Max;
        public static implicit operator Padding(int p) => new() {
            Left = p,
            Top = p,
            Right = p,
            Bottom = p,
        };

    }

    public class PaddingConverter : Style.IConverter {

        public (Type From, Type To)[] Supported { get; } = {
            (typeof(Padding), typeof(int)),
            (typeof(int), typeof(Padding)),
        };

        public T Convert<T>(object raw) {
            if (raw is Padding pad && typeof(T) == typeof(int))
                return (T) (object) pad.Max;
            if (raw is int ltrb && typeof(T) == typeof(Padding))
                return (T) (object) new Padding(ltrb);
            throw new NotSupportedException();
        }

    }
}
