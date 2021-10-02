using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlympUI {
    public class Panel : Group {

        public static readonly new Style DefaultStyle = new() {
            { "Background", new ColorFader(new(0x0a, 0x0a, 0x0a, 0xa0)) },
            { "Border", new ColorFader(new(0x00, 0xad, 0xee, 0xff)) },
            { "BorderSize", new FloatFader(0f) },
            { "Shadow", new FloatFader(1f) },
            { "Radius", new FloatFader(8f) },
            { "Padding", 8 },
            { "Spacing", 8 },
        };

        private BasicMesh BackgroundMesh;
        private BasicMesh? ContentsMesh;
        private RenderTarget2D? Contents;
        private Color PrevBackground;
        private Color PrevBorder;
        private float PrevBorderSize;
        private float PrevShadow;
        private float PrevRadius;
        private bool PrevClip;
        private Point PrevWH;

        public Panel() {
            BackgroundMesh = new BasicMesh(Game.GraphicsDevice) {
                Texture = Assets.GradientQuad,
                WireFrame = false
            };
        }

        protected override void Dispose(bool disposing) {
            if (IsDisposed)
                return;
            base.Dispose(disposing);

            BackgroundMesh?.Dispose();
            ContentsMesh?.Dispose();
            Contents?.Dispose();
        }

        public override void DrawContent() {
            Vector2 xy = ScreenXY;
            Point wh = WH;

            Style.GetCurrent("Background", out Color background);
            Style.GetCurrent("Border", out Color border);
            Style.GetCurrent("BorderSize", out float borderSize);
            Style.GetCurrent("Shadow", out float shadow);
            Style.GetCurrent("Radius", out float radius);

            if (Clip) {
                GraphicsDevice gd = Game.GraphicsDevice;

                if (Contents != null && Contents.IsDisposed)
                    Contents = null;
                if (Contents == null ||
                    PrevWH != wh) {
                    Contents?.Dispose();
                    Contents = new RenderTarget2D(gd, wh.X, wh.Y, false, SurfaceFormat.Color, DepthFormat.None, UI.MultiSampleCount, RenderTargetUsage.PreserveContents);
                }
                if (ContentsMesh == null ||
                    PrevBackground != background ||
                    PrevRadius != radius ||
                    PrevWH != wh) {
                    if (ContentsMesh == null) {
                        ContentsMesh = new BasicMesh(gd) {
                            Texture = new("", () => Contents),
                            WireFrame = false
                        };
                    }
                    ContentsMesh.Texture.Dispose();

                    MeshShapes shapes = ContentsMesh.Shapes;
                    shapes.Clear();

                    shapes.Add(new MeshShapes.Rect() {
                        Color = Color.White,
                        Size = new(wh.X, wh.Y),
                        Radius = radius,
                    });

                    shapes.AutoApply();
                }

                SpriteBatch.End();
                GraphicsStateSnapshot gss = new(gd);
                gd.SetRenderTarget(Contents);
                gd.Clear(background);
                Vector2 offsPrev = UI.TransformOffset;
                UI.TransformOffset = -xy;
                SpriteBatch.BeginUI();

                base.DrawContent();

                SpriteBatch.End();
                gss.Apply();
                UI.TransformOffset = offsPrev;

            } else {
                Contents?.Dispose();
                Contents = null;
                ContentsMesh?.Dispose();
                ContentsMesh = null;
                SpriteBatch.End();
            }

            if (PrevBackground != background ||
                PrevBorder != border ||
                PrevBorderSize != borderSize ||
                PrevShadow != shadow ||
                PrevRadius != radius ||
                PrevClip != Clip ||
                PrevWH != wh) {
                MeshShapes shapes = BackgroundMesh.Shapes;
                shapes.Clear();

                int shadowIndex = -1;
                int shadowEnd = -1;

                if (shadow >= 0.01f) {
                    float shadowWidth = 10f * shadow;
                    shapes.Prepare(out shadowIndex);
                    shapes.Add(new MeshShapes.Rect() {
                        Color = Color.Black * (0.2f + 0.5f * Math.Min(1f, shadow * 0.25f)),
                        XY1 = new(-shadowWidth, -shadowWidth),
                        Size = new(wh.X + shadowWidth * 2f, wh.Y + shadowWidth * 2f),
                        Radius = Math.Max(shadowWidth, radius),
                        Border = shadowWidth,
                    });
                    shadowEnd = shapes.Vertices.Count;

                    // Turn outer edge transparent and move it if necessary.
                    for (int i = shadowIndex; i < shapes.Vertices.Count; i += 2) {
                        VertexPositionColorTexture vertex = shapes.Vertices[i];
                        vertex.Color = Color.Transparent;
                        vertex.TextureCoordinate = new(0f, 0f);
                        // vertex.Position.X += Math.Sign(vertex.Position.X - shadowWidth) * shadowWidth * 0.125f;
                        if (vertex.Position.Y <= shadowWidth) {
                            // vertex.Position.Y += shadowWidth * 0.125f;
                        } else {
                            vertex.Position.Y += shadowWidth * 1.5f;
                        }
                        shapes.Vertices[i] = vertex;
                    }

                    // Fix shadow inner vs rect outer radius gap by adjusting the shadow inner edge to the rect outer edge.
                    // While at it, turn inner edge more transparent at the top.
                    MeshShapes tmp = new() {
                        AutoPoints = shapes.AutoPoints
                    };
                    tmp.Add(new MeshShapes.Rect() {
                        Size = new(wh.X, wh.Y),
                        Radius = Math.Max(0.0001f, radius),
                        Border = 1f,
                    });
                    for (int i = shadowIndex + 1; i < shapes.Vertices.Count; i += 2) {
                        VertexPositionColorTexture vertex = shapes.Vertices[i];
                        if ((shadowWidth * 2f + 8f) > wh.Y && vertex.Position.Y <= shadowWidth + 1)
                            vertex.Color *= 0.9f + 0.1f * Math.Min(1f, shadow - 1f);
                        vertex.TextureCoordinate = new(1f, 1f);
                        vertex.Position = tmp.Vertices[i - shadowIndex - 1].Position;
                        shapes.Vertices[i] = vertex;
                    }
                }

                if (background != default && !Clip) {
                    shapes.Add(new MeshShapes.Rect() {
                        Color = background,
                        Size = new(wh.X, wh.Y),
                        Radius = radius,
                    });
                }

                if (border != default && borderSize >= 0.01f && border.A >= 1) {
                    shapes.Add(new MeshShapes.Rect() {
                        Color = border,
                        Size = new(wh.X, wh.Y),
                        Radius = radius,
                        Border = borderSize,
                    });
                }

                // Fix UVs manually as we're using a gradient texture.
                for (int i = 0; i < shapes.Vertices.Count; i++) {
                    VertexPositionColorTexture vertex = shapes.Vertices[i];
                    if (shadowIndex <= i && i < shadowEnd)
                        continue;
                    vertex.TextureCoordinate = new(1f, 1f);
                    shapes.Vertices[i] = vertex;
                }

                shapes.AutoApply();
            }

            Matrix offs = UI.CreateTransform(xy);
            BackgroundMesh.Draw(offs);
            ContentsMesh?.Draw(offs);

            SpriteBatch.BeginUI();

            if (!Clip)
                base.DrawContent();

            PrevBackground = background;
            PrevBorder = border;
            PrevBorderSize = borderSize;
            PrevShadow = shadow;
            PrevRadius = radius;
            PrevClip = Clip;
            PrevWH = wh;
        }

    }
}
