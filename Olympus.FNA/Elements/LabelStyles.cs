using OlympUI;

namespace Olympus {
    public partial class HeaderBig : Label {

        public static readonly new Style DefaultStyle = new() {
            Assets.FontHeaderBig,
        };
        public HeaderBig(string text)
            : base(text) {
        }

    }

    public partial class HeaderMedium : Label {

        public static readonly new Style DefaultStyle = new() {
            Assets.FontHeaderMedium,
        };
        public HeaderMedium(string text)
            : base(text) {
        }

    }

    public partial class HeaderSmall : Label {

        public static readonly new Style DefaultStyle = new() {
            Assets.FontHeaderSmall,
        };
        public HeaderSmall(string text)
            : base(text) {
        }

    }

    public partial class HeaderSmaller : Label {

        public static readonly new Style DefaultStyle = new() {
            Assets.FontHeaderSmaller,
        };
        public HeaderSmaller(string text)
            : base(text) {
        }

    }

    public partial class LabelSmall : Label {

        public static readonly new Style DefaultStyle = new() {
            OlympUI.Assets.FontSmall,
        };
        public LabelSmall(string text)
            : base(text) {
        }

    }

    public partial class DebugLabel : Label {

        public static readonly new Style DefaultStyle = new() {
            OlympUI.Assets.FontMono,
        };
        public DebugLabel(string text)
            : base(text) {
        }

    }
}
