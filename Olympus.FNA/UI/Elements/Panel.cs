using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OlympUI.MegaCanvas;
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
        private RenderTarget2DRegion? Contents;
        private Color PrevBackground;
        private Color PrevBorder;
        private float PrevBorderSize;
        private float PrevShadow;
        private float PrevRadius;
        private bool PrevClip;
        private Point PrevWH;
        private Point PrevContentsWH;

        public Panel() {
            BackgroundMesh = new(Game.GraphicsDevice) {
                Texture = Assets.GradientQuad
            };
        }

        protected override void Dispose(bool disposing) {
            if (IsDisposed)
                return;
            base.Dispose(disposing);

            BackgroundMesh?.Dispose();
            ContentsMesh?.Dispose();
            Contents?.Dispose();
            Contents = null;
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

                if (Contents != null && (Contents.RT.IsDisposed || Contents.RT.Width < wh.X || Contents.RT.Height < wh.Y)) {
                    Contents?.Dispose();
                    Contents = null;
                }

                if (Contents == null) {
                    Contents = UI.MegaCanvas.GetPooled(wh.X, wh.Y) ?? throw new Exception("Oversized clipped panel!");
                    PrevContentsWH = default;
                }
                Point contentsWH = new(Contents.RT.Width, Contents.RT.Height);

                if (ContentsMesh == null ||
                    PrevBackground != background ||
                    PrevRadius != radius ||
                    PrevContentsWH != contentsWH ||
                    PrevWH != wh) {
                    if (ContentsMesh == null) {
                        ContentsMesh = new BasicMesh(gd) {
                            Texture = new(null, () => Contents.RT)
                        };
                    }
                    ContentsMesh.Texture.Dispose();

                    MeshShapes shapes = ContentsMesh.Shapes;
                    shapes.Clear();

                    shapes.Add(new MeshShapes.Rect() {
                        Color = Color.White,
                        Size = new(wh.X, wh.Y),
                        UVXYMin = new(0, 0),
                        UVXYMax = new(contentsWH.X, contentsWH.Y),
                        Radius = radius,
                    });

                    shapes.AutoApply();
                }

                SpriteBatch.End();
                GraphicsStateSnapshot gss = new(gd);
                gd.SetRenderTarget(Contents.RT);
                if (Contents.Page == null) {
                    gd.Clear(ClearOptions.Target, background, 0, 0);
                } else {
                    gd.ScissorRectangle = Contents.Region;
                    SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.Default, UI.RasterizerStateCullCounterClockwiseScissoredNoMSAA);
                    SpriteBatch.Draw(Assets.White, Contents.Region, background);
                    SpriteBatch.End();
                }
                gd.ScissorRectangle = new(0, 0, wh.X, wh.Y);
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
                    float shadowWidth = 5f * shadow;
                    shapes.Prepare(out shadowIndex, out _);
                    shapes.Add(new MeshShapes.Rect() {
                        Color = Color.Black * (0.2f + 0.5f * Math.Min(1f, shadow * 0.125f)),
                        XY1 = new(-shadowWidth, -shadowWidth),
                        Size = new(wh.X + shadowWidth * 2f, wh.Y + shadowWidth * 2f),
                        Radius = Math.Max(shadowWidth, radius),
                        Border = shadowWidth,
                    });
                    shadowEnd = shapes.VerticesMax;

                    // Turn outer edge transparent and move it if necessary.
                    for (int i = shadowIndex; i < shapes.VerticesMax; i += 2) {
                        ref VertexPositionColorTexture vertex = ref shapes.Vertices[i];
                        vertex.Color = Color.Transparent;
                        vertex.TextureCoordinate = new(0f, 0f);
                        // vertex.Position.X += Math.Sign(vertex.Position.X - shadowWidth) * shadowWidth * 0.125f;
                        if (vertex.Position.Y <= shadowWidth) {
                            // vertex.Position.Y += shadowWidth * 0.125f;
                        } else {
                            vertex.Position.Y += shadowWidth * 1.5f;
                        }
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
                    for (int i = shadowIndex + 1; i < shapes.VerticesMax; i += 2) {
                        ref VertexPositionColorTexture vertex = ref shapes.Vertices[i];
                        if ((shadowWidth * 2f + 8f) > wh.Y && vertex.Position.Y <= shadowWidth + 1)
                            vertex.Color *= 0.9f + 0.1f * Math.Min(1f, shadow - 1f);
                        vertex.TextureCoordinate = new(1f, 1f);
                        vertex.Position = tmp.Vertices[i - shadowIndex - 1].Position;
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
                for (int i = 0; i < shapes.VerticesMax; i++) {
                    ref VertexPositionColorTexture vertex = ref shapes.Vertices[i];
                    if (shadowIndex <= i && i < shadowEnd)
                        continue;
                    vertex.TextureCoordinate = new(1f, 1f);
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
