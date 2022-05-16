using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OlympUI.MegaCanvas;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlympUI {
    public unsafe partial class Panel : Group {

        protected Style.Entry StyleBackground = new(new ColorFader(0x08, 0x08, 0x08, 0xd0));
        protected Style.Entry StyleBorder = new(new ColorFader(0x08, 0x08, 0x08, 0xd0));
        protected Style.Entry StyleBorderSize = new(new FloatFader(0f));
        protected Style.Entry StyleShadow = new(new FloatFader(1f));
        protected Style.Entry StyleRadius = new(new FloatFader(8f));
        protected new Style.Entry StylePadding = new(8);
        protected new Style.Entry StyleSpacing = new(8);

        private BasicMesh BackgroundMesh;
        private BasicMesh BackgroundBlitMesh;
        private BasicMesh? ContentsMesh;
        private IReloadable<RenderTarget2D, Texture2DMeta>? BackgroundMask;
        private IReloadable<RenderTarget2D, Texture2DMeta>? Background;
        private IReloadable<RenderTarget2D, Texture2DMeta>? Contents;
        private Color PrevBackground;
        private Color PrevBorder;
        private float PrevBorderSize;
        private float PrevShadow;
        private float PrevRadius;
        private bool PrevClip;
        private Point PrevWH;
        private Point PrevContentsWH;

        public Panel() {
            BackgroundMesh = new(Game) {
                Texture = Assets.GradientQuadY
            };

            BackgroundBlitMesh = new(Game) {
                Texture = Assets.White,
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
            Vector2? offsPrev = null;
            Vector2 xy = ScreenXY;
            Point wh = WH;

            SpriteBatch.End();

            StyleBackground.GetCurrent(out Color background);
            StyleBorder.GetCurrent(out Color border);
            StyleBorderSize.GetCurrent(out float borderSize);
            StyleShadow.GetCurrent(out float shadow);
            StyleRadius.GetCurrent(out float radius);

            int padding = (int) MathF.Ceiling(10 * shadow);
            Point whPadded = new(wh.X + padding * 2, wh.Y + padding * 2);

            bool maskUpdated = false;

            {
                if (BackgroundMask is not null && (!Clip || BackgroundMask.ValueValid is not { } rt || rt.Width < wh.X || rt.Height < wh.Y)) {
                    BackgroundMask?.Dispose();
                    Background?.Dispose();
                    BackgroundMask = null;
                    Background = null;
                    BackgroundBlitMesh.Texture = Assets.White;
                }
            }

            {
                if (Background is not null && (Background.ValueValid is not { } rt || rt.Width < whPadded.X || rt.Height < whPadded.Y)) {
                    BackgroundMask?.Dispose();
                    Background?.Dispose();
                    BackgroundMask = null;
                    Background = null;
                    BackgroundBlitMesh.Texture = Assets.White;
                }
            }

            if (BackgroundMask is null || Background is null) {
                if (BackgroundMask is null && Clip) {
                    BackgroundMask = Reloadable.Temporary(default(RenderTarget2DRegionMeta), () => UI.MegaCanvas.PoolMSAA.Get(wh.X, wh.Y), true).UnpackRT(true);
                }
                if (Background is null) {
                    Background = Reloadable.Temporary(default(RenderTarget2DRegionMeta), () => UI.MegaCanvas.PoolMSAA.Get(whPadded.X, whPadded.Y), true).UnpackRT(true);
                    BackgroundBlitMesh.Texture = new ReloadableAs<RenderTarget2D, Texture2D, Texture2DMeta>(Background);
                }
                PrevWH = default;
            }

            if (PrevBackground != background ||
                PrevBorder != border ||
                PrevBorderSize != borderSize ||
                PrevShadow != shadow ||
                PrevRadius != radius ||
                PrevClip != Clip ||
                PrevWH != wh) {
                MeshShapes<MiniVertex> shapes = BackgroundMesh.Shapes;
                shapes.Clear();

                gss ??= new(gd);

                if (Clip) {
                    // BackgroundMask is created earlier in this method if it's null and if clipping is enabled.
                    Debug.Assert(BackgroundMask is not null);

                    shapes.Add(new MeshShapes.Rect() {
                        Color = new(1f, 1f, 1f, 1f),
                        Size = new(wh.X, wh.Y),
                        Radius = radius,
                    });

                    // Fix UVs manually as we're using a gradient texture.
                    for (int i = 0; i < shapes.VerticesMax; i++) {
                        ref MiniVertex vertex = ref shapes.Vertices[i];
                        vertex.UV = new(1f, 1f);
                    }

                    shapes.AutoApply();

                    RenderTarget2D rt = BackgroundMask.Value;
                    rt.SetRenderTargetUsage(RenderTargetUsage.PlatformContents);
                    gd.SetRenderTarget(rt);
                    gd.Clear(ClearOptions.Target, new Vector4(0, 0, 0, 0), 0, 0);
                    rt.SetRenderTargetUsage(RenderTargetUsage.PreserveContents);

                    BackgroundMesh.Draw(BackgroundMesh.CreateTransform());
                    maskUpdated = true;
                }

                shapes.Clear();

                int shadowIndex = -1;
                int shadowEnd = -1;

                if (shadow >= 0.01f) {
                    float shadowWidth = 8f * shadow;
                    shapes.Prepare(out shadowIndex, out _);
                    shapes.Add(new MeshShapes.Rect() {
                        Color = Color.Black * (0.1f + 0.3f * Math.Min(1f, shadow * 0.125f)),
                        XY1 = new(-shadowWidth, -shadowWidth),
                        Size = new(wh.X + shadowWidth * 2f, wh.Y + shadowWidth * 2f),
                        Radius = Math.Max(shadowWidth, radius),
                        Border = shadowWidth,
                    });
                    shadowEnd = shapes.VerticesMax;

                    // Turn outer edge transparent and move it if necessary.
                    for (int i = shadowIndex; i < shapes.VerticesMax; i += 2) {
                        ref MiniVertex vertex = ref shapes.Vertices[i];
                        vertex.Color = Color.Transparent;
                        vertex.UV = new(0f, 0f);
                        // vertex.Position.X += Math.Sign(vertex.Position.X - shadowWidth) * shadowWidth * 0.125f;
                        if (vertex.Y <= shadowWidth) {
                            // vertex.Y += shadowWidth * 0.5f;
                        } else {
                            vertex.Y += shadowWidth * 1.5f;
                        }
                    }

                    // Fix shadow inner vs rect outer radius gap by adjusting the shadow inner edge to the rect outer edge.
                    // While at it, turn inner edge more transparent at the top.
                    MeshShapes<MiniVertex> tmp = new() {
                        AutoPoints = shapes.AutoPoints
                    };
                    tmp.Add(new MeshShapes.Rect() {
                        Size = new(wh.X, wh.Y),
                        Radius = Math.Max(0.0001f, radius),
                        Border = 1f,
                    });
                    for (int i = shadowIndex + 1; i < shapes.VerticesMax; i += 2) {
                        ref MiniVertex vertex = ref shapes.Vertices[i];
                        ref MiniVertex gen = ref tmp.Vertices[i - shadowIndex - 1];
                        if ((shadowWidth * 2f + 8f) > wh.Y && (float) vertex.Y <= shadowWidth + 1)
                            vertex.Color *= 0.9f + 0.1f * Math.Min(1f, shadow - 1f);
                        vertex.UV = new(1f, 1f);
                        vertex.X = gen.X;
                        vertex.Y = gen.Y;
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
                    ref MiniVertex vertex = ref shapes.Vertices[i];
                    if (shadowIndex <= i && i < shadowEnd)
                        continue;
                    vertex.UV = new(1f, 1f);
                }

                shapes.AutoApply();

                {
                    RenderTarget2D rt = Background.Value;
                    rt.SetRenderTargetUsage(RenderTargetUsage.PlatformContents);
                    gd.SetRenderTarget(rt);
                    gd.Clear(ClearOptions.Target, new Vector4(0, 0, 0, 0), 0, 0);
                    rt.SetRenderTargetUsage(RenderTargetUsage.PreserveContents);

                    BackgroundMesh.Draw(BackgroundMesh.CreateTransform(new(padding, padding)));

                    fixed (MiniVertex* vertices = &BackgroundBlitMesh.Vertices[0]) {
                        Vector2 uv = new(whPadded.X / (float) rt.Width, whPadded.Y / (float) rt.Height);
                        vertices[0].XY = new(0, 0);
                        vertices[0].UV = new(0, 0);
                        vertices[1].XY = new(whPadded.X, 0);
                        vertices[1].UV = new(uv.X, 0);
                        vertices[2].XY = new(0, whPadded.Y);
                        vertices[2].UV = new(0, uv.Y);
                        vertices[3].XY = new(whPadded.X, whPadded.Y);
                        vertices[3].UV = new(uv.X, uv.Y);
                    }
                    BackgroundBlitMesh.QueueNext();
                }
            }

            if (Clip) {
                if (Contents is not null && (Contents.ValueValid is not { } rt || rt.Width < wh.X || rt.Height < wh.Y)) {
                    Contents?.Dispose();
                    Contents = null;
                    if (ContentsMesh is not null)
                        ContentsMesh.Texture = Assets.White;
                }

                if (Contents is null) {
                    Contents = Reloadable.Temporary(default(RenderTarget2DRegionMeta), () => UI.MegaCanvas.PoolMSAA.Get(wh.X, wh.Y), true).UnpackRT(true);
                    PrevContentsWH = default;
                }
                Point contentsWH = new(Contents.Meta.Width, Contents.Meta.Height);

                if (ContentsMesh is null ||
                    PrevBackground != background ||
                    PrevRadius != radius ||
                    PrevContentsWH != contentsWH ||
                    PrevWH != wh ||
                    maskUpdated) {
                    if (ContentsMesh is null) {
                        ContentsMesh = new BasicMesh(Game) {
                            Effect = MaskEffect.Cache.GetEffect(() => Game.GraphicsDevice),
                            Texture = Assets.White,
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
                    ContentsMesh.Texture = new ReloadableAs<RenderTarget2D, Texture2D, Texture2DMeta>(Contents);

                    fixed (MiniVertex* vertices = &ContentsMesh.Vertices[0]) {
                        Vector2 uv = new(wh.X / (float) Contents.Meta.Width, wh.Y / (float) Contents.Meta.Height);
                        vertices[0].XY = new(0, 0);
                        vertices[0].UV = new(0, 0);
                        vertices[1].XY = new(wh.X, 0);
                        vertices[1].UV = new(uv.X, 0);
                        vertices[2].XY = new(0, wh.Y);
                        vertices[2].UV = new(0, uv.Y);
                        vertices[3].XY = new(wh.X, wh.Y);
                        vertices[3].UV = new(uv.X, uv.Y);
                    }
                    ContentsMesh.QueueNext();
                }

                gss ??= new(gd);

                rt = Contents.Value;
                rt.SetRenderTargetUsage(RenderTargetUsage.PlatformContents);
                gd.SetRenderTarget(rt);
                gd.Clear(ClearOptions.Target, new Vector4(0, 0, 0, 0), 0, 0);
                rt.SetRenderTargetUsage(RenderTargetUsage.PreserveContents);
                offsPrev = UI.TransformOffset;
                UI.TransformOffset = -xy;
                SpriteBatch.BeginUI();

                base.DrawContent();

                SpriteBatch.End();

            } else {
                Contents?.Dispose();
                Contents = null;
                ContentsMesh?.Dispose();
                ContentsMesh = null;
            }

            gss?.Apply();
            if (offsPrev is not null)
                UI.TransformOffset = offsPrev.Value;

            BackgroundBlitMesh.Draw(UI.CreateTransform(new(xy.X - padding, xy.Y - padding)));
            if (Contents is not null && ContentsMesh is not null) {
                // BackgroundMask is created earlier in this method if it's null and if clipping is enabled.
                Debug.Assert(BackgroundMask is not null);
                gd.Textures[1] = BackgroundMask.Value;
                ((MaskEffect) ContentsMesh.Effect.Value).MaskXYWH = new(
                    0, 0,
                    Contents.Meta.Width / (float) BackgroundMask.Meta.Width, Contents.Meta.Height / (float) BackgroundMask.Meta.Height
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
