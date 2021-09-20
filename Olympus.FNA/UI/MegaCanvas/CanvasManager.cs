using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlympUI.MegaCanvas {
    public sealed class CanvasManager : IDisposable {

        public readonly GraphicsDevice Graphics;

        public int MinSize = 8;
        public int MaxSize = 4096;
        public int PageSize = 4096;
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
                            entry.RT.Dispose();
                            PoolTotalMemory -= entry.RT.GetMemoryUsage();
                            PoolEntries[i] = default;
                            PoolEntriesAlive--;
                        }
                    }
                    for (int i = PoolEntries.Length - 1; i >= PoolCullTarget; --i) {
                        PoolEntry entry = PoolEntries[i];
                        if (!entry.IsNull) {
                            entry.RT.Dispose();
                            PoolTotalMemory -= entry.RT.GetMemoryUsage();
                            PoolEntries[i] = default;
                            PoolEntriesAlive--;
                        }
                    }

                } else {
                    for (int i = PoolEntries.Length - 1; i >= 0; --i) {
                        ref PoolEntry entry = ref PoolEntries[i];
                        if (!entry.IsDisposed && entry.Age++ >= PoolMaxAgeFrames) {
                            entry.RT.Dispose();
                            PoolTotalMemory -= entry.RT.GetMemoryUsage();
                            PoolEntries[i] = default;
                            PoolEntriesAlive--;
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
            GraphicsDevice gd = UI.Game.GraphicsDevice;
            GraphicsStateSnapshot gss = new(gd);

            gd.SetRenderTarget(to);
            using SpriteBatch sb = new(gd);
            sb.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise);
            sb.Draw(from, toBounds, fromBounds, Color.White);
            sb.End();

            gss.Apply();
        }

        public RenderTarget2DRegion? Pack(RenderTarget2D rt) {
            RenderTarget2DRegion? rtrg = GetRegion(new(0, 0, rt.Width, rt.Height));
            if (rtrg == null)
                return null;

            Blit(rt, new Rectangle(0, 0, rt.Width, rt.Height), rtrg.RT, rtrg.Region);
            return rtrg;
        }

        public RenderTarget2DRegion? GetRegion(Rectangle want) {
            if (want.Width > PageSize || want.Height > PageSize)
                return null;
            foreach (AtlasPage page in Pages)
                if (page.GetRegion(want) is RenderTarget2DRegion rtrg)
                    return rtrg;
            // FIXME: Add new pages?
            return null;
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

        public RenderTarget2DRegion? GetPooled(int width, int height) {
            if (width < MinSize || height < MinSize ||
                width > MaxSize || height > MaxSize)
                return null;
            
            RenderTarget2D? rt = null;
            bool fresh;

            lock (PoolEntries) {
                if (PoolEntries.TryGetSmallest(width, height, out PoolEntry entry, out int index)) {
                    PoolEntries[index] = default;
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
