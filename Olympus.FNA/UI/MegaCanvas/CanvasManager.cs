using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlympUI.MegaCanvas {
    public sealed class CanvasManager : IDisposable {

        public readonly GraphicsDevice Graphics;

        public int MinSize = 8;
        public int MaxSize = 4096;
        public int PageSize = 4096;
        public int MaxPackedSize = 2048;
        public int MultiSampleCount = 0;

        public readonly PoolEntry[] PoolEntries = new PoolEntry[64];
        public int PoolEntriesAlive = 0;
        public readonly HashSet<RenderTarget2D> PoolUsed = new();
        public long PoolUsedMemory = 0;
        public long PoolTotalMemory = 0;
        public int PoolPadding = 128;
        private int PoolSquishFrame = 0;
        public int PoolSquishFrames = 60 * 15;
        public int PoolMaxAgeFrames = 60 * 5;
        public int PoolCullTarget = 32;
        public bool PoolCullTriggered = false;

        public readonly List<AtlasPage> Pages = new();

        public CanvasManager(GraphicsDevice graphics) {
            Graphics = graphics;
        }

        public void Dispose() {
            for (int i = 0; i < PoolEntries.Length; i++) {
                PoolEntries[i].RT?.Dispose();
                PoolEntries[i] = default;
            }
            PoolEntriesAlive = 0;
            PoolUsedMemory = 0;
            PoolTotalMemory = 0;
            PoolSquishFrame = 0;

            foreach (AtlasPage page in Pages)
                page.Dispose();
            Pages.Clear();
        }

        public void Update() {
            lock (PoolEntries) {
                if (PoolSquishFrame++ >= PoolSquishFrames || PoolCullTriggered) {
                    for (int i = PoolEntries.Length - 1; i >= 0; --i) {
                        ref PoolEntry entry = ref PoolEntries[i];
                        if (!entry.IsNull && (entry.IsDisposed || entry.Age++ >= PoolMaxAgeFrames)) {
                            Free(ref entry);
                        }
                    }
                    for (int i = PoolEntries.Length - 1; i >= PoolCullTarget; --i) {
                        PoolEntry entry = PoolEntries[i];
                        if (!entry.IsNull) {
                            Free(ref entry);
                        }
                    }

                } else {
                    for (int i = PoolEntries.Length - 1; i >= 0; --i) {
                        ref PoolEntry entry = ref PoolEntries[i];
                        if (!entry.IsDisposed && entry.Age++ >= PoolMaxAgeFrames) {
                            Free(ref entry);
                        }
                    }
                }
            }

            lock (Pages) {
                foreach (AtlasPage page in Pages) {
                    page.Update();
                }
            }
        }

        public void Blit(Texture2D from, Rectangle fromBounds, RenderTarget2D to, Rectangle toBounds) {
            GraphicsDevice gd = Graphics;
            GraphicsStateSnapshot gss = new(gd);

            gd.SetRenderTarget(to);
            using SpriteBatch sb = new(gd);
            sb.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);
            sb.Draw(from, toBounds, fromBounds, Color.White);
            sb.End();

            gss.Apply();
        }

        public RenderTarget2DRegion? GetPacked(RenderTarget2D old)
            => GetPacked(old, new(0, 0, old.Width, old.Height));

        public RenderTarget2DRegion? GetPacked(RenderTarget2D old, Rectangle oldBounds) {
            RenderTarget2DRegion? packed = GetRegion(oldBounds);
            if (packed == null)
                return null;

            Blit(old, oldBounds, packed.RT, packed.Region);
            return packed;
        }

        public RenderTarget2DRegion? GetPackedAndFree(RenderTarget2DRegion old)
            => GetPackedAndFree(old, old.Region);

        public RenderTarget2DRegion? GetPackedAndFree(RenderTarget2DRegion old, Rectangle oldBounds) {
            RenderTarget2DRegion? packed = GetPacked(old.RT, oldBounds);
            if (packed == null)
                return null;

            Free(old);
            return packed;
        }

        public RenderTarget2DRegion? GetRegion(Rectangle want) {
            if (want.Width > MaxPackedSize || want.Height > MaxPackedSize)
                return null;

            {
                foreach (AtlasPage page in Pages)
                    if (page.GetRegion(want) is RenderTarget2DRegion rtrg)
                        return rtrg;
            }

            {
                AtlasPage page = new(this);
                Pages.Add(page);
                return page.GetRegion(want);
            }
        }

        public void Free(RenderTarget2DRegion? rtrg) {
            if (rtrg == null)
                return;

            if (rtrg.Page == null) {
                FreePooled(rtrg.RT);
                return;
            }

            rtrg.Page.Free(rtrg);
        }

        private void Free(ref PoolEntry entry) {
            entry.RT?.Dispose();
            PoolTotalMemory -= entry.RT?.GetMemoryUsage() ?? 0;
            entry = default;
            PoolEntriesAlive--;
        }

        public RenderTarget2DRegion? GetPooled(int width, int height) {
            if (width < MinSize || height < MinSize ||
                width > MaxSize || height > MaxSize)
                return null;
            
            RenderTarget2D? rt = null;
            bool fresh;

            lock (PoolEntries) {
                if (PoolEntries.TryGetSmallest(width, height, out PoolEntry entry, out int index)) {
                    PoolEntries[index] = default;
                    PoolEntriesAlive--;
                    rt = entry.RT;
                }
            }

            if (rt == null) {
                int widthReal = Math.Min(MaxSize, (int) MathF.Ceiling(width / PoolPadding + 1) * PoolPadding);
                int heightReal = Math.Min(MaxSize, (int) MathF.Ceiling(height / PoolPadding + 1) * PoolPadding);
                rt = new(Graphics, widthReal, heightReal, false, SurfaceFormat.Color, DepthFormat.None, MultiSampleCount, RenderTargetUsage.PreserveContents);
                fresh = true;

            } else {
                fresh = false;
            }

            lock (PoolEntries) {
                PoolUsed.Add(rt);
                PoolUsedMemory += rt.GetMemoryUsage();
                if (fresh)
                    PoolTotalMemory += rt.GetMemoryUsage();
            }

            return new(rt);
        }

        public void FreePooled(RenderTarget2D? rt) {
            if (rt == null)
                return;
            lock (PoolEntries) {
                PoolUsed.Remove(rt);
                PoolUsedMemory -= rt.Width * rt.Height * 4;

                if (!rt.IsDisposed) {
                    if (PoolCullTriggered) {
                        rt.Dispose();
                        PoolTotalMemory -= rt.GetMemoryUsage();

                    } else if (PoolEntriesAlive >= PoolEntries.Length) {
                        PoolCullTriggered = true;
                        rt.Dispose();
                        PoolTotalMemory -= rt.GetMemoryUsage();

                    } else {
                        for (int i = 0; i < PoolEntries.Length; i++) {
                            if (PoolEntries[i].IsNull) {
                                PoolEntries[i] = new(rt);
                                PoolEntriesAlive++;
                                return;
                            }
                        }
                        // This shouldn't ever be reached but eh.
                        PoolCullTriggered = true;
                        rt.Dispose();
                        PoolTotalMemory -= rt.GetMemoryUsage();
                    }
                }
            }
        }

        public void Dump(string dir) {
            GraphicsDevice gd = Graphics;
            GraphicsStateSnapshot gss = new(gd);
            Texture2D white = Assets.White;

            for (int i = Pages.Count - 1; i >= 0; --i) {
                AtlasPage page = Pages[i];
                RenderTarget2D rt = page.RT;
                using RenderTarget2D tmp = new(gd, rt.Width, rt.Height, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);

                gd.SetRenderTarget(tmp);
                using SpriteBatch sb = new(gd);
                sb.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);
                sb.Draw(rt, new Vector2(0f, 0f), Color.White);
                sb.End();
                sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);
                foreach (Rectangle rg in page.Spaces) {
                    sb.Draw(white, new Rectangle(rg.X, rg.Y, rg.Width, 1), Color.Green * 0.7f);
                    sb.Draw(white, new Rectangle(rg.X, rg.Bottom - 1, rg.Width, 1), Color.Green * 0.7f);
                    sb.Draw(white, new Rectangle(rg.X, rg.Y + 1, 1, rg.Height - 2), Color.Green * 0.7f);
                    sb.Draw(white, new Rectangle(rg.Right - 1, rg.Y + 1, 1, rg.Height - 2), Color.Green * 0.7f);
                    sb.Draw(white, new Rectangle(rg.X + 1, rg.Y + 1, rg.Width - 2, rg.Height - 2), Color.Green * 0.3f);
                }
                foreach (RenderTarget2DRegion rtrg in page.Taken) {
                    Rectangle rg = rtrg.Region;
                    sb.Draw(white, new Rectangle(rg.X, rg.Y, rg.Width, 1), Color.Blue * 0.7f);
                    sb.Draw(white, new Rectangle(rg.X, rg.Bottom - 1, rg.Width, 1), Color.Blue * 0.7f);
                    sb.Draw(white, new Rectangle(rg.X, rg.Y + 1, 1, rg.Height - 2), Color.Blue * 0.7f);
                    sb.Draw(white, new Rectangle(rg.Right - 1, rg.Y + 1, 1, rg.Height - 2), Color.Blue * 0.7f);
                    sb.Draw(white, new Rectangle(rg.X + 1, rg.Y + 1, rg.Width - 2, rg.Height - 2), Color.Blue * 0.3f);
                }
                sb.End();

                using FileStream fs = new(Path.Combine(dir, $"atlas_{i}.png"), FileMode.Create);
                tmp.SaveAsPng(fs, tmp.Width, tmp.Height);
            }

            gss.Apply();

            for (int i = PoolEntries.Length - 1; i >= 0; --i) {
                PoolEntry entry = PoolEntries[i];
                if (entry.IsDisposed)
                    continue;
                using FileStream fs = new(Path.Combine(dir, $"pooled_{i}.png"), FileMode.Create);
                entry.RT.SaveAsPng(fs, entry.RT.Width, entry.RT.Height);
            }
        }

        public struct PoolEntry : ISizeable {
            
            public RenderTarget2D? RT;

            public int Width { get; set; }
            public int Height { get; set; }

            public int Age;

            [MemberNotNullWhen(false, nameof(RT))]
            public bool IsDisposed => RT?.IsDisposed ?? true;
            [MemberNotNullWhen(false, nameof(RT))]
            public bool IsNull => RT == null;

            public PoolEntry(RenderTarget2D rt) {
                RT = rt;
                Width = rt.Width;
                Height = rt.Height;
                Age = 0;
            }

        }

    }
}
