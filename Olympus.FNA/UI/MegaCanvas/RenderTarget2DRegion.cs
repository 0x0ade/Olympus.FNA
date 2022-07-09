using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Threading;

namespace OlympUI.MegaCanvas {
    public sealed class RenderTarget2DRegion : IDisposable {
        
        private static uint TotalRegionCount;

        public readonly uint UniqueID;
        public readonly CanvasManager Manager;
        public readonly CanvasPool? Pool;
        public readonly AtlasPage? Page;
        public readonly RenderTarget2D RT;
        public readonly Rectangle Region;
        public readonly Rectangle UsedRegion;

        public bool IsDisposed { get; private set; }

        public RenderTarget2DRegion(CanvasPool pool, RenderTarget2D rt, Rectangle region, Rectangle usedRegion) {
            UniqueID = Interlocked.Increment(ref TotalRegionCount);
            Manager = pool.Manager;
            Pool = pool;
            RT = rt;
            Region = region;
            UsedRegion = usedRegion;
        }

        public RenderTarget2DRegion(AtlasPage page, RenderTarget2D rt, Rectangle region, Rectangle usedRegion) {
            UniqueID = Interlocked.Increment(ref TotalRegionCount);
            Manager = page.Manager;
            Page = page;
            RT = rt;
            Region = region;
            UsedRegion = usedRegion;
        }

        public void Dispose() {
            if (IsDisposed)
                return;
            IsDisposed = true;

            if (!UI.IsOnMainThread) {
                Manager.Queue(() => Dispose());
                return;
            }

            if (Pool is not null) {
                Pool.Free(RT);
                return;
            }

            if (Page is not null) {
                Page.Free(this);
                return;
            }

            RT.Dispose();
        }

    }

    public struct RenderTarget2DRegionMeta : IReloadableMeta<RenderTarget2DRegion> {

        public bool IsValid(RenderTarget2DRegion? value) => value is not null && !value.IsDisposed;

    }

    public static class RenderTarget2DRegionReloadableExtensions {

        public static IReloadable<RenderTarget2D, Texture2DMeta> UnpackRT(this IReloadable<RenderTarget2DRegion, RenderTarget2DRegionMeta> target, bool owns)
            => new ReloadableLink<RenderTarget2DRegion, RenderTarget2DRegionMeta, RenderTarget2D, Texture2DMeta>(
                target, _ => new Texture2DMeta(target.Value.RT, null), value => value.RT, owns ? target => target.Dispose() : _ => { });

    }
}
