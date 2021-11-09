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
    public unsafe class Panel : Group {

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
        private BasicMesh BackgroundBlitMesh;
        private BasicMesh? ContentsMesh;
        private RenderTarget2DRegion? BackgroundMask;
        private RenderTarget2DRegion? Background;
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

            BackgroundBlitMesh = new(Game.GraphicsDevice) {
                Texture = new(null, () => Background?.RT),
                // Will be updated in Draw.
                Shapes = {
                    new MeshShapes.Quad() {
                        XY1 = new(0, 0),
                        XY2 = new(1, 0),
                        XY3 = new(0, 1),
                        XY4 = new(1, 1),
                        UV1 = new(0, 0),
                        UV2 = new(1, 0),
                        UV3 = new(0, 1),
                        UV4 = new(1, 1),
                    },
                },
                MSAA = false,
            };
            BackgroundBlitMesh.Reload();
        }

        protected override void Dispose(bool disposing) {
            if (IsDisposed)
                return;
            base.Dispose(disposing);

            BackgroundMesh?.Dispose();
            BackgroundBlitMesh?.Dispose();
            ContentsMesh?.Dispose();
            BackgroundMask?.Dispose();
            Background?.Dispose();
            Contents?.Dispose();
            Contents = null;
        }

        public override void DrawContent() {
            GraphicsDevice gd = Game.GraphicsDevice;
            GraphicsStateSnapshot? gss = null;
            Vector2 xy = ScreenXY;
            Point wh = WH;

            SpriteBatch.End();

            Style.GetCurrent(out Color background);
            Style.GetCurrent(out Color border);
            Style.GetCurrent(out float borderSize);
            Style.GetCurrent(out float shadow);
            Style.GetCurrent(out float radius);

            int padding = (int) MathF.Ceiling(10 * shadow);
            Point whPadded = new(wh.X + padding * 2, wh.Y + padding * 2);

            bool maskUpdated = false;

            if (Background != null && (Background.RT.IsDisposed || Background.RT.Width < wh.X || Background.RT.Height < wh.Y)) {
                BackgroundMask?.Dispose();
                Background?.Dispose();
                BackgroundMask = null;
                Background = null;
            }

            if (BackgroundMask == null || Background == null) {
                BackgroundMask = UI.MegaCanvas.PoolMSAA.Get(wh.X, wh.Y) ?? throw new Exception("Oversized clipped panel!");
                Background = UI.MegaCanvas.PoolMSAA.Get(whPadded.X, whPadded.Y) ?? throw new Exception("Oversized clipped panel!");
                PrevWH = default;
            }

            if (PrevBackground != background ||
                PrevBorder != border ||
                PrevBorderSize != borderSize ||
                PrevShadow != shadow ||
                PrevRadius != radius ||
                PrevClip != Clip ||
                PrevWH != wh) {
                BackgroundBlitMesh.Texture.Dispose();
                MeshShapes shapes = BackgroundMesh.Shapes;
                shapes.Clear();

                gss ??= new(gd);

                if (Clip) {
                    shapes.Add(new MeshShapes.Rect() {
                        Color = new(1f, 1f, 1f, 1f),
                        Size = new(wh.X, wh.Y),
                        Radius = radius,
                    });

                    // Fix UVs manually as we're using a gradient texture.
                    for (int i = 0; i < shapes.VerticesMax; i++) {
                        ref VertexPositionColorTexture vertex = ref shapes.Vertices[i];
                        vertex.TextureCoordinate = new(1f, 1f);
                    }

                    shapes.AutoApply();

                    BackgroundMask.RT.SetRenderTargetUsage(RenderTargetUsage.PlatformContents);
                    gd.SetRenderTarget(BackgroundMask.RT);
                    gd.Clear(ClearOptions.Target, new Vector4(0, 0, 0, 0), 0, 0);
                    BackgroundMask.RT.SetRenderTargetUsage(RenderTargetUsage.PreserveContents);

                    BackgroundMesh.Draw(BackgroundMesh.CreateTransform());
                    maskUpdated = true;
                }

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

                if (background != default) {
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

                Background.RT.SetRenderTargetUsage(RenderTargetUsage.PlatformContents);
                gd.SetRenderTarget(Background.RT);
                gd.Clear(ClearOptions.Target, new Vector4(0, 0, 0, 0), 0, 0);
                Background.RT.SetRenderTargetUsage(RenderTargetUsage.PreserveContents);

                BackgroundMesh.Draw(BackgroundMesh.CreateTransform(new(padding, padding)));

                fixed (VertexPositionColorTexture* vertices = &BackgroundBlitMesh.Vertices[0]) {
                    Vector2 uv = new(whPadded.X / (float) Background.RT.Width, whPadded.Y / (float) Background.RT.Height);
                    vertices[0].Position = new(0, 0, 0);
                    vertices[0].TextureCoordinate = new(0, 0);
                    vertices[1].Position = new(whPadded.X, 0, 0);
                    vertices[1].TextureCoordinate = new(uv.X, 0);
                    vertices[2].Position = new(0, whPadded.Y, 0);
                    vertices[2].TextureCoordinate = new(0, uv.Y);
                    vertices[3].Position = new(whPadded.X, whPadded.Y, 0);
                    vertices[3].TextureCoordinate = new(uv.X, uv.Y);
                }
                BackgroundBlitMesh.QueueNext();
            }

            if (Clip) {
                if (Contents != null && (Contents.RT.IsDisposed || Contents.RT.Width < wh.X || Contents.RT.Height < wh.Y)) {
                    Contents?.Dispose();
                    Contents = null;
                }

                if (Contents == null) {
                    Contents = UI.MegaCanvas.PoolMSAA.Get(wh.X, wh.Y) ?? throw new Exception("Oversized clipped panel!");
                    PrevContentsWH = default;
                }
                Point contentsWH = new(Contents.RT.Width, Contents.RT.Height);

                if (ContentsMesh == null ||
                    PrevBackground != background ||
                    PrevRadius != radius ||
                    PrevContentsWH != contentsWH ||
                    PrevWH != wh ||
                    maskUpdated) {
                    if (ContentsMesh == null) {
                        ContentsMesh = new BasicMesh(gd) {
                            Effect = Assets.MaskEffect,
                            Texture = new(null, () => Contents.RT),
                            // Will be updated afterwards.
                            Shapes = {
                                new MeshShapes.Quad() {
                                    XY1 = new(0, 0),
                                    XY2 = new(1, 0),
                                    XY3 = new(0, 1),
                                    XY4 = new(1, 1),
                                    UV1 = new(0, 0),
                                    UV2 = new(1, 0),
                                    UV3 = new(0, 1),
                                    UV4 = new(1, 1),
                                },
                            },
                            MSAA = false,
                        };
                        ContentsMesh.Reload();
                    }
                    ContentsMesh.Texture.Dispose();

                    fixed (VertexPositionColorTexture* vertices = &ContentsMesh.Vertices[0]) {
                        Vector2 uv = new(wh.X / (float) Contents.RT.Width, wh.Y / (float) Contents.RT.Height);
                        vertices[0].Position = new(0, 0, 0);
                        vertices[0].TextureCoordinate = new(0, 0);
                        vertices[1].Position = new(wh.X, 0, 0);
                        vertices[1].TextureCoordinate = new(uv.X, 0);
                        vertices[2].Position = new(0, wh.Y, 0);
                        vertices[2].TextureCoordinate = new(0, uv.Y);
                        vertices[3].Position = new(wh.X, wh.Y, 0);
                        vertices[3].TextureCoordinate = new(uv.X, uv.Y);
                    }
                    ContentsMesh.QueueNext();
                }

                gss ??= new(gd);

                Contents.RT.SetRenderTargetUsage(RenderTargetUsage.PlatformContents);
                gd.SetRenderTarget(Contents.RT);
                gd.Clear(ClearOptions.Target, new Vector4(0, 0, 0, 0), 0, 0);
                Contents.RT.SetRenderTargetUsage(RenderTargetUsage.PreserveContents);
                Vector2 offsPrev = UI.TransformOffset;
                UI.TransformOffset = -xy;
                SpriteBatch.BeginUI();

                base.DrawContent();

                SpriteBatch.End();
                UI.TransformOffset = offsPrev;

            } else {
                Contents?.Dispose();
                Contents = null;
                ContentsMesh?.Dispose();
                ContentsMesh = null;
            }

            gss?.Apply();

            BackgroundBlitMesh.Draw(UI.CreateTransform(new(xy.X - padding, xy.Y - padding)));
            if (Contents != null && ContentsMesh != null) {
                gd.Textures[1] = BackgroundMask.RT;
                ((MaskEffect) ContentsMesh.Effect).MaskXYWH = new(
                    0, 0,
                    Contents.RT.Width / (float) BackgroundMask.RT.Width, Contents.RT.Height / (float) BackgroundMask.RT.Height
                );
                ContentsMesh.Draw(UI.CreateTransform(xy));
                gd.Textures[1] = null;
            }

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
