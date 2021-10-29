using FontStashSharp;
using Microsoft.Xna.Framework;
using OlympUI;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Olympus {
    public class HeaderBig : Label {

        public static readonly new Style DefaultStyle = new() {
            Assets.FontHeaderBig,
        };
        public HeaderBig(string text)
            : base(text) {
        }

    }

    public class HeaderMedium : Label {

        public static readonly new Style DefaultStyle = new() {
            Assets.FontHeaderMedium,
        };
        public HeaderMedium(string text)
            : base(text) {
        }

    }

    public class HeaderSmall : Label {

        public static readonly new Style DefaultStyle = new() {
            Assets.FontHeaderSmall,
        };
        public HeaderSmall(string text)
            : base(text) {
        }

    }

    public class HeaderSmaller : Label {

        public static readonly new Style DefaultStyle = new() {
            Assets.FontHeaderSmaller,
        };
        public HeaderSmaller(string text)
            : base(text) {
        }

    }

    public class DebugLabel : Label {

        public static readonly new Style DefaultStyle = new() {
            OlympUI.Assets.FontMono,
        };
        public DebugLabel(string text)
            : base(text) {
        }

    }
}
