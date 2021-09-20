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
    public sealed class MeshShapes : IEnumerable {

        public readonly List<VertexPositionColorTexture> Vertices = new();
        public readonly List<ushort> Indices = new();

        public Color Color = Color.White;
        public int AutoPoints = 32;

        public BasicMesh? Mesh;

        public static Vector2 GetNormal(Vector2 a, Vector2 b)
            => Vector2.Normalize(new(b.Y - a.Y, a.X - b.X));

        public void Clear() {
            Vertices.Clear();
            Indices.Clear();
        }

        public void Apply(BasicMesh mesh) {
            Mesh = mesh;
            mesh.Vertices = Vertices.ToArray();
            mesh.Indices = Indices.ToArray();
            mesh.Reload();
        }

        public MeshShapes Optimize() {
            VertexPositionColorTexture[] verticesOld = Vertices.ToArray();
            ushort[] indicesOld = Indices.ToArray();
            Dictionary<VertexPositionColorTexture, ushort> verticesMap = new();
            ushort[] indicesMap = new ushort[verticesOld.Length];

            for (int i = 0; i < verticesOld.Length; i++) {
                VertexPositionColorTexture vertex = verticesOld[i];
                if (verticesMap.TryGetValue(vertex, out ushort found)) {
                    indicesMap[i] = found;
                } else {
                    verticesMap.Add(vertex, (ushort) i);
                    indicesMap[i] = (ushort) i;
                    Vertices.Add(vertex);
                }
            }

            foreach (int i in indicesOld)
                Indices.Add(indicesMap[i]);

            return this;
        }

        public void AutoApply() {
            if (Mesh == null)
                return;

            Mesh.QueueReload(Vertices, Indices);
        }

        public void Prepare(out int index) {
            index = Vertices.Count;
        }

        public void Prepare(Color colorIn, out int index, out Color color) {
            Prepare(out index);

            if (colorIn == default) {
                color = Color;
            } else {
                color = colorIn.Multiply(Color);
            }
        }

        public void Add(Raw shape) {
            int index = Vertices.Count;
            int indexIndex = Indices.Count;

            Vertices.AddRange(shape.Vertices);

            Indices.AddRange(shape.Indices);
            for (int i = Indices.Count - 1; i >= indexIndex; --i)
                Indices[i] = (ushort) (Indices[i] + index);

            AutoApply();
        }

        public void Add(Poly shape) {
            Prepare(shape.Color, out int index, out Color color);

            if (shape.Width != 0f) {
                // Luckily lines are somewhat easy to deal with...
                foreach (VertexPositionColorTexture vertex in shape)
                    Vertices.Add(new VertexPositionColorTexture(vertex.Position, color, vertex.TextureCoordinate));

                int count = (Vertices.Count - index) / 2;
                for (int i = 0; i < count - 1; i++) {
                    Indices.Add((ushort) (index + 2 * i + 2));
                    Indices.Add((ushort) (index + 2 * i + 1));
                    Indices.Add((ushort) (index + 2 * i + 0));

                    Indices.Add((ushort) (index + 2 * i + 1));
                    Indices.Add((ushort) (index + 2 * i + 2));
                    Indices.Add((ushort) (index + 2 * i + 3));
                }

                AutoApply();
                return;
            }

            // TODO: Eventually replace LibTessDotNet with a built-in triangulator / tesselator?
            Tess tess = new();

            ContourVertex[] contour = new ContourVertex[shape.XYs.Count];
            int contourIndex = 0;
            foreach (VertexPositionColorTexture vertex in shape)
                contour[contourIndex++] = new() {
                    Position = new(vertex.Position.X, vertex.Position.Y, vertex.Position.Z),
                    Data = new VertexPositionColorTexture(vertex.Position, color, vertex.TextureCoordinate)
                };

            tess.AddContour(contour);

            tess.Tessellate(combineCallback: (pos, data, weights) => {
                VertexPositionColorTexture[] input = {
                    (VertexPositionColorTexture) data[0],
                    (VertexPositionColorTexture) data[1],
                    (VertexPositionColorTexture) data[2],
                    (VertexPositionColorTexture) data[3]
                };
                return new VertexPositionColorTexture(
                    new(pos.X, pos.Y, pos.Z),
                    new(
                        (byte) (input[0].Color.R * weights[0] + input[1].Color.R * weights[1] + input[2].Color.R * weights[2] + input[3].Color.R * weights[3]),
                        (byte) (input[0].Color.G * weights[0] + input[1].Color.G * weights[1] + input[2].Color.G * weights[2] + input[3].Color.G * weights[3]),
                        (byte) (input[0].Color.B * weights[0] + input[1].Color.B * weights[1] + input[2].Color.B * weights[2] + input[3].Color.B * weights[3]),
                        (byte) (input[0].Color.A * weights[0] + input[1].Color.A * weights[1] + input[2].Color.A * weights[2] + input[3].Color.A * weights[3])
                    ),
                    new(
                        (byte) (input[0].TextureCoordinate.X * weights[0] + input[1].TextureCoordinate.X * weights[1] + input[2].TextureCoordinate.X * weights[2] + input[3].TextureCoordinate.X * weights[3]),
                        (byte) (input[0].TextureCoordinate.Y * weights[0] + input[1].TextureCoordinate.Y * weights[1] + input[2].TextureCoordinate.Y * weights[2] + input[3].TextureCoordinate.Y * weights[3])
                    )
                );
            });

            foreach (ContourVertex vertex in tess.Vertices)
                Vertices.Add((VertexPositionColorTexture) vertex.Data);

            int triangles = tess.ElementCount;
            for (int i = 0; i < triangles; i++) {
                Indices.Add((ushort) (index + tess.Elements[i * 3 + 0]));
                Indices.Add((ushort) (index + tess.Elements[i * 3 + 1]));
                Indices.Add((ushort) (index + tess.Elements[i * 3 + 2]));
            }

            AutoApply();
        }

        public void Add(Quad shape) {
            Prepare(shape.Color, out int index, out Color color);

            if (!shape.HasUV) {
                Vertices.Add(new(new(shape.XY1, 0f), color, new Vector2(0f, 0f)));
                Vertices.Add(new(new(shape.XY2, 0f), color, new Vector2(1f, 0f)));
                Vertices.Add(new(new(shape.XY3, 0f), color, new Vector2(0f, 1f)));
                Vertices.Add(new(new(shape.XY4, 0f), color, new Vector2(1f, 1f)));
            } else {
                Vertices.Add(new(new(shape.XY1, 0f), color, shape.UV1));
                Vertices.Add(new(new(shape.XY2, 0f), color, shape.UV2));
                Vertices.Add(new(new(shape.XY3, 0f), color, shape.UV3));
                Vertices.Add(new(new(shape.XY4, 0f), color, shape.UV4));
            }

            Indices.Add((ushort) (index + 0));
            Indices.Add((ushort) (index + 1));
            Indices.Add((ushort) (index + 2));

            Indices.Add((ushort) (index + 3));
            Indices.Add((ushort) (index + 2));
            Indices.Add((ushort) (index + 1));

            AutoApply();
        }

        public void Add(Rect shape) {
            float border = shape.Border;
            bool isBorder = border != 0f;
            bool isRound =
                shape.RadiusTL != 0f || shape.RadiusTR != 0f ||
                shape.RadiusBL != 0f || shape.RadiusBR != 0f;

            Vector2 min = shape.XY1;
            Vector2 max = shape.XY2;
            Vector2 size = max - min;

            if (!isBorder && !isRound) {
                Add(new Quad() {
                    Color = shape.Color,
                    XY1 = new(min.X, min.Y),
                    XY2 = new(max.X, min.Y),
                    XY3 = new(min.X, max.Y),
                    XY4 = new(max.X, max.Y)
                });
                return;
            }

            if (isBorder && !isRound) {
                // Top
                Add(new Quad() {
                    Color = shape.Color,
                    XY1 = new(min.X,            min.Y),
                    XY2 = new(max.X,            min.Y),
                    XY3 = new(min.X,            min.Y + border),
                    XY4 = new(max.X,            min.Y + border),
                    UVXYMin = min,
                    UVXYMax = max
                });
                // Bottom
                Add(new Quad() {
                    Color = shape.Color,
                    XY1 = new(min.X,            max.Y - border),
                    XY2 = new(max.X,            max.Y - border),
                    XY3 = new(min.X,            max.Y),
                    XY4 = new(max.X,            max.Y),
                    UVXYMin = min,
                    UVXYMax = max
                });
                // Left
                Add(new Quad() {
                    Color = shape.Color,
                    XY1 = new(min.X,            min.Y + border),
                    XY2 = new(min.X + border,   min.Y + border),
                    XY3 = new(min.X,            max.Y - border),
                    XY4 = new(min.X + border,   max.Y - border),
                    UVXYMin = min,
                    UVXYMax = max
                });
                // Right
                Add(new Quad() {
                    Color = shape.Color,
                    XY1 = new(max.X - border,   min.Y + border),
                    XY2 = new(max.X,            min.Y + border),
                    XY3 = new(max.X - border,   max.Y - border),
                    XY4 = new(max.X,            max.Y - border),
                    UVXYMin = min,
                    UVXYMax = max
                });
                return;
            }

            Poly poly = new() {
                Color = shape.Color,
                Width = shape.Border,
                UVXYMin = min,
                UVXYMax = max
            };

            int pointsPerBend = shape.RadiusPoints;
            if (pointsPerBend == 0) {
                if (shape.RadiusTL != 0f)
                    pointsPerBend++;
                if (shape.RadiusTR != 0f)
                    pointsPerBend++;
                if (shape.RadiusBL != 0f)
                    pointsPerBend++;
                if (shape.RadiusBR != 0f)
                    pointsPerBend++;
                pointsPerBend = AutoPoints / pointsPerBend;
            }

            min += new Vector2(border * 0.5f, border * 0.5f);
            max -= new Vector2(border * 0.5f, border * 0.5f);

            float pointsPerBendPerStep = (pointsPerBend + 2f) * 2f;
            pointsPerBend += 2;
            float radiusMin = Math.Min((size.X - border) * 0.5f, (size.Y - border) * 0.5f);
            float angle;
            float radius;
            Vector2 last = default;

            angle = MathF.PI * 0.0f;

            radius = Math.Min(shape.RadiusTL, radiusMin);
            for (int i = 0; i < pointsPerBend; i++)
                poly.XYs.Add(last = new(
                    min.X + radius * (1f - MathF.Cos(angle + i * MathF.PI / pointsPerBendPerStep)),
                    min.Y + radius * (1f - MathF.Sin(angle + i * MathF.PI / pointsPerBendPerStep))
                ));

            angle = MathF.PI * 0.5f;
            poly.XYs[^1] = new(last.X, min.Y);

            radius = Math.Min(shape.RadiusTR, radiusMin);
            for (int i = 0; i < pointsPerBend; i++)
                poly.XYs.Add(last = new(
                    max.X - radius * (1f + MathF.Cos(angle + i * MathF.PI / pointsPerBendPerStep)),
                    min.Y + radius * (1f - MathF.Sin(angle + i * MathF.PI / pointsPerBendPerStep))
                ));

            angle = MathF.PI * 1.0f;
            poly.XYs[^1] = new(max.X, last.Y);

            radius = Math.Min(shape.RadiusBR, radiusMin);
            for (int i = 0; i < pointsPerBend; i++)
                poly.XYs.Add(last = new(
                    max.X - radius * (1f + MathF.Cos(angle + i * MathF.PI / pointsPerBendPerStep)),
                    max.Y - radius * (1f + MathF.Sin(angle + i * MathF.PI / pointsPerBendPerStep))
                ));

            angle = MathF.PI * 1.5f;
            poly.XYs[^1] = new(last.X, max.Y);

            radius = Math.Min(shape.RadiusBL, radiusMin);
            for (int i = 0; i < pointsPerBend; i++)
                poly.XYs.Add(last = new(
                    min.X + radius * (1f - MathF.Cos(angle + i * MathF.PI / pointsPerBendPerStep)),
                    max.Y - radius * (1f + MathF.Sin(angle + i * MathF.PI / pointsPerBendPerStep))
                ));

            angle = MathF.PI * 0.0f;
            poly.XYs[^1] = new(min.X, last.Y);

            poly.XYs.Add(poly.XYs[0]);

            Add(poly);
        }

        public void Add(Line shape) {
            Vector2 normal = GetNormal(shape.XY1, shape.XY2) * (shape.Width != 0f ? shape.Width : Line.DefaultWidth) * 0.5f;
            Add(new Quad() {
                Color = shape.Color,
                XY1 = shape.XY1 + normal,
                XY2 = shape.XY2 + normal,
                XY3 = shape.XY1 - normal,
                XY4 = shape.XY2 - normal
            });
        }

        public IEnumerator GetEnumerator()
            => throw new CompilerSatisfactionException();

        public struct Raw {
            public VertexPositionColorTexture[] Vertices;
            public ushort[] Indices;
        }

        public enum LineCornerType {
            Cut,
            Extend
        }

        public class Poly : IEnumerable<VertexPositionColorTexture> {
            public Color Color;

            public List<Vector2> XYs = new();
            public float Width;

            public Vector2 UVXYMin;
            public Vector2 UVXYMax;
            public Vector2 UVXYSize {
                get => UVXYMax - UVXYMin;
                set => UVXYMax = UVXYMin + value;
            }

            public LineCornerType LineCornerType;

            public void Add(Color color)
                => Color = color;

            public void Add(float width)
                => Width = width;

            public void Add(Vector2 xy)
                => XYs.Add(xy);

            IEnumerator IEnumerable.GetEnumerator()
                => GetEnumerator();

            public IEnumerator<VertexPositionColorTexture> GetEnumerator() {
                Color color = Color != default ? Color : Color.White;
                float width = Width * 0.5f;

                Vector2 min = UVXYMin;
                Vector2 max = UVXYMax;
                if (min == default && max == default) {
                    min = max = XYs[0];
                    for (int i = 1; i < XYs.Count - 1; i++) {
                        Vector2 xy = XYs[i];
                        min.X = Math.Min(min.X, xy.X);
                        min.Y = Math.Min(min.Y, xy.Y);
                        max.X = Math.Max(max.X, xy.X);
                        max.Y = Math.Max(max.Y, xy.Y);
                    }
                    min = new(min.X - width, min.Y - width);
                    max = new(max.X + width, max.Y + width);
                }
                Vector2 size = new(max.X - min.X, max.Y - min.Y);
                Vector2 sizeInv = new(1f / size.X, 1f / size.Y);
                Vector2 minSizeInv = min / size;

                if (width == 0f) {
                    for (int i = 0; i < XYs.Count; i++) {
                        Vector2 xy = XYs[i];
                        yield return new(new(xy, 0f), color, xy * sizeInv - minSizeInv);
                    }

                } else {
                    Vector2 xy = XYs[0];
                    Vector2 startN = GetNormal(xy, XYs[1]);
                    Vector2 startD = Vector2.Normalize(XYs[1] - xy);
                    Vector2 normal = startN;
                    Vector2 normalW = normal * width;
                    Vector2 dir = startD;
                    Vector2 startT = xy + normalW;
                    Vector2 startB = xy - normalW;
                    xy = XYs[^1];
                    Vector2 endN = normal = GetNormal(XYs[^2], xy);
                    normalW = normal * width;
                    Vector2 endD = dir = Vector2.Normalize(xy - XYs[^2]);
                    Vector2 endT = xy + normalW;
                    Vector2 endB = xy - normalW;

                    if (xy == XYs[0]) {
                        // Push vertices out of overlaps using intersection points
                        Vector2 iT = UIMath.Intersect(
                            startT, startT + startD,
                            endT, endT + endD
                        );
                        Vector2 iB = UIMath.Intersect(
                            startB, startB + startD,
                            endB, endB + endD
                        );
                        if (!float.IsNaN(iT.X) && !float.IsNaN(iT.Y) &&
                            !float.IsNaN(iB.X) && !float.IsNaN(iB.Y)) {
                            switch (LineCornerType) {
                                case LineCornerType.Cut:
                                default:
                                    if (MathF.Atan2(startD.X, startD.Y) < MathF.Atan2(endD.X, endD.Y)) {
                                        startT = endT = iT;
                                    } else {
                                        startB = endB = iB;
                                    }
                                    break;

                                case LineCornerType.Extend:
                                    startT = endT = iT;
                                    startB = endB = iB;
                                    break;
                            }
                        }
                    }

                    normal = startN;
                    normalW = normal * width;
                    dir = startD;
                    yield return new(new(startT, 0f), color, startT * sizeInv - minSizeInv);
                    yield return new(new(startB, 0f), color, startB * sizeInv - minSizeInv);

                    for (int i = 1; i < XYs.Count - 1; i++) {
                        xy = XYs[i];
                        Vector2 pN = normal;
                        Vector2 pD = dir;
                        Vector2 pT = xy + normalW;
                        Vector2 pB = xy - normalW;
                        Vector2 nN = normal = GetNormal(xy, XYs[i + 1]);
                        normalW = normal * width;
                        Vector2 nD = dir = Vector2.Normalize(XYs[i + 1] - xy);
                        Vector2 nT = xy + normalW;
                        Vector2 nB = xy - normalW;

                        // Push vertices out of overlaps using intersection points
                        Vector2 iT = UIMath.Intersect(
                            pT, pT + pD,
                            nT, nT + nD
                        );
                        Vector2 iB = UIMath.Intersect(
                            pB, pB + pD,
                            nB, nB + nD
                        );
                        if (!float.IsInfinity(iT.X) && !float.IsInfinity(iT.Y) &&
                            !float.IsInfinity(iB.X) && !float.IsInfinity(iB.Y)) {
                            switch (LineCornerType) {
                                case LineCornerType.Cut:
                                default:
                                    if (MathF.Atan2(pD.X, pD.Y) < MathF.Atan2(nD.X, nD.Y)) {
                                        pT = nT = iT;
                                    } else {
                                        pB = nB = iB;
                                    }
                                    break;

                                case LineCornerType.Extend:
                                    pT = nT = iT;
                                    pB = nB = iB;
                                    break;
                            }
                        }

                        yield return new(new(pT, 0f), color, pT * sizeInv - minSizeInv);
                        yield return new(new(pB, 0f), color, pB * sizeInv - minSizeInv);
                        yield return new(new(nT, 0f), color, nT * sizeInv - minSizeInv);
                        yield return new(new(nB, 0f), color, nB * sizeInv - minSizeInv);
                    }

                    yield return new(new(endT, 0f), color, endT * sizeInv - minSizeInv);
                    yield return new(new(endB, 0f), color, endB * sizeInv - minSizeInv);

                    if (XYs[^1] == XYs[0]) {
                        yield return new(new(startT, 0f), color, startT * sizeInv - minSizeInv);
                        yield return new(new(startB, 0f), color, startB * sizeInv - minSizeInv);
                    }
                }
            }
        }

        public struct Quad {
            public Color Color;
            private Vector2 _XY1;
            public Vector2 XY1 {
                get => _XY1;
                set {
                    _XY1 = value;
                    RecalcUV();
                }
            }
            private Vector2 _XY2;
            public Vector2 XY2 {
                get => _XY2;
                set {
                    _XY2 = value;
                    RecalcUV();
                }
            }
            private Vector2 _XY3;
            public Vector2 XY3 {
                get => _XY3;
                set {
                    _XY3 = value;
                    RecalcUV();
                }
            }
            private Vector2 _XY4;
            public Vector2 XY4 {
                get => _XY4;
                set {
                    _XY4 = value;
                    RecalcUV();
                }
            }
            public Vector2 UV1;
            public Vector2 UV2;
            public Vector2 UV3;
            public Vector2 UV4;
            public bool HasUV => UV1 != default || UV2 != default || UV3 != default || UV4 != default;
            private Vector2 _UVXYMin;
            private Vector2 _UVXYMax;
            public Vector2 UVXYMin {
                get => _UVXYMin;
                set {
                    _UVXYMin = value;
                    RecalcUV();
                }
            }
            public Vector2 UVXYMax {
                get => _UVXYMax;
                set {
                    _UVXYMax = value;
                    RecalcUV();
                }
            }
            public Vector2 UVXYSize {
                get => UVXYMax - UVXYMin;
                set => UVXYMax = UVXYMin + value;
            }
            private void RecalcUV() {
                if (_UVXYMin == default && _UVXYMax == default) {
                    UV1 = default;
                    UV2 = default;
                    UV3 = default;
                    UV4 = default;
                    return;
                }

                Vector2 min = _UVXYMin;
                Vector2 size = _UVXYMax - min;
                UV1 = (XY1 - min) / size;
                UV2 = (XY2 - min) / size;
                UV3 = (XY3 - min) / size;
                UV4 = (XY4 - min) / size;
            }
        }

        public struct Rect {
            public Color Color;
            public Vector2 XY1;
            public Vector2 XY2;
            public Vector2 Size {
                get => XY2 - XY1;
                set => XY2 = XY1 + value;
            }
            public float Border;
            public float RadiusTL;
            public float RadiusTR;
            public float RadiusBL;
            public float RadiusBR;
            public float Radius {
                get => Math.Max(Math.Max(RadiusTL, RadiusTR), Math.Max(RadiusBL, RadiusBR));
                set {
                    RadiusTL = value;
                    RadiusTR = value;
                    RadiusBL = value;
                    RadiusBR = value;
                }
            }
            public int RadiusPoints;
        }

        public struct Line {
            public static float DefaultWidth = 8f;
            public Color Color;
            public Vector2 XY1;
            public Vector2 XY2;
            public float Width;
        }

    }
}
