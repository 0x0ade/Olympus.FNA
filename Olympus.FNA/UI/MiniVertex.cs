using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Runtime.InteropServices;

namespace OlympUI {
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public record struct MiniVertex : IVertexType {

        private static VertexDeclaration _VertexDeclaration = new(
            new VertexElement(
                0,
                VertexElementFormat.Vector2,
                VertexElementUsage.Position,
                0
            ),
            new VertexElement(
                8,
                VertexElementFormat.Vector2,
                VertexElementUsage.TextureCoordinate,
                0
            ),
            new VertexElement(
                16,
                VertexElementFormat.Color,
                VertexElementUsage.Color,
                0
            )
        );

        public VertexDeclaration VertexDeclaration => _VertexDeclaration;

        public float X;
        public float Y;

        public Vector2 XY {
            get => new(X, Y);
            set => _ = (X = value.X, Y = value.Y);
        }

        public Vector3 XY0 {
            get => new(X, Y, 0f);
            set => _ = (X = value.X, Y = value.Y);
        }

        public float U;
        public float V;

        public Vector2 UV {
            get => new(U, V);
            set => _ = (U = value.X, V = value.Y);
        }

        public Color Color;

    }

    public sealed class MiniVertexGenerator : VertexGenerator<MiniVertex> {

        public override MiniVertex New(Vector3 position, Color color, Vector2 textureCoordinate) {
            return new() {
                X = position.X,
                Y = position.Y,
                U = textureCoordinate.X,
                V = textureCoordinate.Y,
                Color = color
            };
        }

        public override MiniVertex Apply(MiniVertex vertex, Vector3? position, Color? color, Vector2? textureCoordinate) {
            return new() {
                X = position?.X ?? vertex.X,
                Y = position?.Y ?? vertex.Y,
                U = textureCoordinate?.X ?? vertex.U,
                V = textureCoordinate?.Y ?? vertex.V,
                Color = color ?? vertex.Color
            };
        }

        public override Vector3 GetPosition(MiniVertex vertex) {
            return vertex.XY0;
        }

        public override MiniVertex Interpolate(Vector3 pos, Span<MiniVertex> input, Span<float> weights) {
            return new() {
                X = pos.X,
                Y = pos.Y,
                U = (input[0].U * weights[0] + input[1].U * weights[1] + input[2].U * weights[2] + input[3].U * weights[3]),
                V = (input[0].V * weights[0] + input[1].V * weights[1] + input[2].V * weights[2] + input[3].V * weights[3]),
                Color = new(
                    (byte) (input[0].Color.R * weights[0] + input[1].Color.R * weights[1] + input[2].Color.R * weights[2] + input[3].Color.R * weights[3]),
                    (byte) (input[0].Color.G * weights[0] + input[1].Color.G * weights[1] + input[2].Color.G * weights[2] + input[3].Color.G * weights[3]),
                    (byte) (input[0].Color.B * weights[0] + input[1].Color.B * weights[1] + input[2].Color.B * weights[2] + input[3].Color.B * weights[3]),
                    (byte) (input[0].Color.A * weights[0] + input[1].Color.A * weights[1] + input[2].Color.A * weights[2] + input[3].Color.A * weights[3])
                )
            };
        }

    }
}
