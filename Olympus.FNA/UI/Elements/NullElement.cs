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
    public sealed partial class NullElement : Element {

        protected override bool IsComposited => false;

        public override void Update(float dt) {
        }

        public override void UpdateHidden(float dt) {
        }

        public override void Paint() {
        }

        public override IReloadable<RenderTarget2DRegion, RenderTarget2DRegionMeta>? PaintToCache(Padding padding) {
            return null;
        }

    }
}
