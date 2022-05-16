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
    public partial class BlurryGroup : Group {

        protected Style.Entry StyleRadius = new(new FloatFader(0f));
        protected Style.Entry StyleStrength = new(new FloatFader(1f));
        protected Style.Entry StyleScale = new(new FloatFader(1f));
        protected Style.Entry StyleNoise = new(new FloatFader(0f));

        public override bool? Cached => true;
        public override bool Clip => true;
        public override Padding ClipExtend => new(0);

        private uint PrevCachedPaintID;

        private RenderTarget2DRegion? BlurredX;
        private RenderTarget2DRegion? BlurredXY;
        private RenderTarget2DRegion? Noised;
        private RenderTarget2DRegion? Display;

        private Rectangle DisplayBounds;

        public BlurryGroup() {
        }

        protected override void Dispose(bool disposing) {
            if (IsDisposed)
                return;
            base.Dispose(disposing);

            BlurredX?.Dispose();
            BlurredXY?.Dispose();
            Noised?.Dispose();
        }

        protected override void DrawCachedTexture(SpriteBatch spriteBatch, RenderTarget2D rt, Vector2 xy, Padding padding, Rectangle region) {
            StyleRadius.GetCurrent(out int radius);
            if (radius <= 0) {
                base.DrawCachedTexture(spriteBatch, rt, xy, padding, region);
                return;
            }

            StyleScale.GetCurrent(out float scale);
            Point whFull = WH;
            Point whMin = new((int) (whFull.X / scale), (int) (whFull.Y / scale));

            RenderTarget2DRegion? display = Display;
            if (PrevCachedPaintID != CachedPaintID || display is null) {
                PrevCachedPaintID = CachedPaintID;

                GraphicsDevice gd = Game.GraphicsDevice;

                spriteBatch.End();
                GraphicsStateSnapshot gss = new(gd);
                Vector2 offsPrev = UI.TransformOffset;
                UI.TransformOffset = new(0, 0);

                StyleStrength.GetCurrent(out float strength);
                StyleNoise.GetCurrent(out float noise);

                BlurEffect blurrer = (BlurEffect) BlurEffect.Cache.GetEffect(() => gd, radius).Value;
                blurrer.Radius = radius;
                blurrer.Strength = strength;

                RenderTarget2DRegion? blurredX = BlurredX;
                if (blurredX is not null && (blurredX.RT.IsDisposed || blurredX.RT.Width < whMin.X || blurredX.RT.Height < whFull.Y)) {
                    blurredX?.Dispose();
                    BlurredX = blurredX = null;
                }
                if (blurredX is null) {
                    BlurredX = blurredX = (CachePool ?? (MSAA ? UI.MegaCanvas.PoolMSAA : UI.MegaCanvas.Pool)).Get(whMin.X, whFull.Y);
                }
                if (blurredX is null) {
                    throw new Exception($"{nameof(BlurryGroup)} tried obtaining blurred X render target but got null");
                }

                RenderTarget2DRegion? blurredXY = BlurredXY;
                if (blurredXY is not null && (blurredXY.RT.IsDisposed || blurredXY.RT.Width < whMin.X || blurredXY.RT.Height < whMin.Y)) {
                    blurredXY?.Dispose();
                    BlurredXY = blurredXY = null;
                }
                if (blurredXY is null) {
                    BlurredXY = blurredXY = (CachePool ?? (MSAA ? UI.MegaCanvas.PoolMSAA : UI.MegaCanvas.Pool)).Get(whMin.X, whMin.Y);
                }
                if (blurredXY is null) {
                    throw new Exception($"{nameof(BlurryGroup)} tried obtaining blurred XY render target but got null");
                }

                blurredX.RT.SetRenderTargetUsage(RenderTargetUsage.PlatformContents);
                gd.SetRenderTarget(blurredX.RT);
                blurrer.Axis = BlurAxis.X;
                blurrer.Transform = UI.CreateTransform();
                blurredX.RT.SetRenderTargetUsage(RenderTargetUsage.PreserveContents);
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, blurrer);
                spriteBatch.Draw(rt, new Rectangle(0, 0, whMin.X, whFull.Y), region, Color.White);
                spriteBatch.End();

                blurredXY.RT.SetRenderTargetUsage(RenderTargetUsage.PlatformContents);
                gd.SetRenderTarget(blurredXY.RT);
                blurrer.Axis = BlurAxis.Y;
                blurrer.Transform = UI.CreateTransform();
                blurredXY.RT.SetRenderTargetUsage(RenderTargetUsage.PreserveContents);
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, blurrer);
                spriteBatch.Draw(blurredX.RT, new Rectangle(0, 0, whMin.X, whMin.Y), new Rectangle(0, 0, whMin.X, whFull.Y), Color.White);
                spriteBatch.End();

                if (noise >= 0f) {
                    RenderTarget2DRegion? noised = Noised;
                    if (noised is not null && (noised.RT.IsDisposed || noised.RT.Width != whFull.X || noised.RT.Height != whFull.Y)) {
                        noised?.Dispose();
                        Noised = noised = null;
                    }
                    if (noised is null) {
                        Noised = noised = (CachePool ?? (MSAA ? UI.MegaCanvas.PoolMSAA : UI.MegaCanvas.Pool)).Get(whFull.X, whFull.Y);
                    }
                    if (noised is null) {
                        throw new Exception($"{nameof(BlurryGroup)} tried obtaining noised render target but got null");
                    }

                    NoiseEffect noiser = (NoiseEffect) NoiseEffect.Cache.GetEffect(() => gd).Value;
                    noiser.Spread = new(
                        noise * (whMin.X / (float) noised.RT.Width) * 0.01f,
                        noise * (whMin.Y / (float) noised.RT.Height) * 0.01f
                    );
                    noiser.Blend = noise * 0.8f;

                    noised.RT.SetRenderTargetUsage(RenderTargetUsage.PlatformContents);
                    gd.SetRenderTarget(noised.RT);
                    noiser.Transform = UI.CreateTransform();
                    noised.RT.SetRenderTargetUsage(RenderTargetUsage.PreserveContents);
                    spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise, noiser);
                    spriteBatch.Draw(blurredXY.RT, new Rectangle(0, 0, whFull.X, whFull.Y), new Rectangle(0, 0, whMin.X, whMin.Y), Color.White);
                    spriteBatch.End();

                    Display = display = noised;
                    DisplayBounds = new(0, 0, whFull.X, whFull.Y);
                } else {
                    Noised?.Dispose();
                    Noised = null;
                    Display = display = blurredXY;
                    DisplayBounds = new(0, 0, whMin.X, whMin.Y);
                }

                gss.Apply();
                UI.TransformOffset = offsPrev;
                spriteBatch.BeginUI();
            }

            spriteBatch.Draw(
                display.RT,
                new Rectangle(
                    (int) xy.X - padding.Left,
                    (int) xy.Y - padding.Top,
                    region.Width,
                    region.Height
                ),
                DisplayBounds,
                Color.White
            );
        }

    }
}
