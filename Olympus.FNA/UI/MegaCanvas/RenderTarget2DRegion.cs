using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlympUI.MegaCanvas {
    public sealed class RenderTarget2DRegion : IDisposable {

        public readonly CanvasManager? Manager;
        public readonly AtlasPage? Page;
        public readonly RenderTarget2D RT;
        public readonly Rectangle Region;

        public RenderTarget2DRegion(CanvasManager? manager, RenderTarget2D rt) {
            Manager = manager;
            Page = null;
            RT = rt;
            Region = new(0, 0, rt.Width, rt.Height);
        }

        public RenderTarget2DRegion(CanvasManager? manager, AtlasPage? page, RenderTarget2D rt, Rectangle region) {
            Manager = manager;
            Page = page;
            RT = rt;
            Region = region;
        }

        public void Dispose() {
            if (Manager == null) {
                RT.Dispose();
                return;
            }

            if (Page == null) {
                Manager.FreePooled(RT);
                return;
            }

            Page.Free(this);
        }

    }
}
