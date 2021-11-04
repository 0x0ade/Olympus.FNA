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
            if (!Manager.IsOnMainThread) {
                Manager.Queue(() => Dispose());
                return;
            }

            if (Pool != null) {
                Pool.Free(RT);
                return;
            }

            if (Page != null) {
                Page.Free(this);
                return;
            }

            RT.Dispose();
        }

    }
}
