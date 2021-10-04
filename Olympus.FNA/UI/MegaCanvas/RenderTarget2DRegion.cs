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

        public readonly AtlasPage? Page;
        public readonly RenderTarget2D RT;
        public readonly Rectangle Region;

        public RenderTarget2DRegion(RenderTarget2D rt) {
            Page = null;
            RT = rt;
            Region = new(0, 0, rt.Width, rt.Height);
        }

        public RenderTarget2DRegion(AtlasPage? page, RenderTarget2D rt, Rectangle region) {
            Page = page;
            RT = rt;
            Region = region;
        }

        public void Dispose() {
            RT.Dispose();
            Page?.Free(this);
        }

    }
}
