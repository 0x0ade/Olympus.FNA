using FontStashSharp;
using LibTessDotNet;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OlympUI {
    public static class VertexGenerator {

        public static VertexGenerator<TVertex> Get<TVertex>() where TVertex : unmanaged, IVertexType {
            if (VertexGenerator<TVertex>.Default is { } gen)
                return gen;

            foreach (Type type in UIReflection.GetAllTypes(typeof(VertexGenerator<TVertex>))) {
                if (type.IsAbstract)
                    continue;

                return VertexGenerator<TVertex>.Default = (VertexGenerator<TVertex>?) Activator.CreateInstance(type)!;
            }

            throw new Exception($"Couldn't find VertexGenerator for {typeof(TVertex)}");
        }

    }

    public abstract class VertexGenerator<TVertex> where TVertex : unmanaged, IVertexType {

        public static VertexGenerator<TVertex>? Default;

        public abstract TVertex New(Vector3 position, Color color, Vector2 textureCoordinate);

        public abstract TVertex Apply(TVertex vertex, Vector3? position, Color? color, Vector2? textureCoordinate);

        public abstract Vector3 GetPosition(TVertex vertex);

        public abstract TVertex Interpolate(Vector3 position, Span<TVertex> input, Span<float> weights);

    }

    public sealed class VertexPositionColorTextureGenerator : VertexGenerator<VertexPositionColorTexture> {

        public override VertexPositionColorTexture New(Vector3 position, Color color, Vector2 textureCoordinate) {
            return new(position, color, textureCoordinate);
        }

        public override VertexPositionColorTexture Apply(VertexPositionColorTexture vertex, Vector3? position, Color? color, Vector2? textureCoordinate) {
            return new(position ?? vertex.Position, color ?? vertex.Color, textureCoordinate ?? vertex.TextureCoordinate);
        }

        public override Vector3 GetPosition(VertexPositionColorTexture vertex) {
            return vertex.Position;
        }

        public override VertexPositionColorTexture Interpolate(Vector3 pos, Span<VertexPositionColorTexture> input, Span<float> weights) {
            return new(
                new(pos.X, pos.Y, pos.Z),
                new(
                    (byte) (input[0].Color.R * weights[0] + input[1].Color.R * weights[1] + input[2].Color.R * weights[2] + input[3].Color.R * weights[3]),
                    (byte) (input[0].Color.G * weights[0] + input[1].Color.G * weights[1] + input[2].Color.G * weights[2] + input[3].Color.G * weights[3]),
                    (byte) (input[0].Color.B * weights[0] + input[1].Color.B * weights[1] + input[2].Color.B * weights[2] + input[3].Color.B * weights[3]),
                    (byte) (input[0].Color.A * weights[0] + input[1].Color.A * weights[1] + input[2].Color.A * weights[2] + input[3].Color.A * weights[3])
                ),
                new(
                    input[0].TextureCoordinate.X * weights[0] + input[1].TextureCoordinate.X * weights[1] + input[2].TextureCoordinate.X * weights[2] + input[3].TextureCoordinate.X * weights[3],
                    input[0].TextureCoordinate.Y * weights[0] + input[1].TextureCoordinate.Y * weights[1] + input[2].TextureCoordinate.Y * weights[2] + input[3].TextureCoordinate.Y * weights[3]
                )
            );
        }

    }
}
