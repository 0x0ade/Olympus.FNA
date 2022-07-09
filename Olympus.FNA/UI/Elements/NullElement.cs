using OlympUI.MegaCanvas;

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
