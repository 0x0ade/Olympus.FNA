using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OlympUI.MegaCanvas {
    public sealed class RenderTarget2DRegion : IDisposable {

        public readonly CanvasManager Manager;
        public readonly CanvasPool? Pool;
        public readonly AtlasPage? Page;
        public readonly RenderTarget2D RT;
        public readonly Rectangle Region;

        public bool IsDisposed { get; private set; }

        public RenderTarget2DRegion(CanvasPool pool, RenderTarget2D rt, Rectangle region) {
            Manager = pool.Manager;
            Pool = pool;
            RT = rt;
            Region = region;
        }

        public RenderTarget2DRegion(AtlasPage page, RenderTarget2D rt, Rectangle region) {
            Manager = page.Manager;
            Page = page;
            RT = rt;
            Region = region;
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
