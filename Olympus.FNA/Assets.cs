using FontStashSharp;
using OlympUI;
using static OlympUI.Assets;

namespace Olympus {
    public static class Assets {

        public static readonly Reloadable<DynamicSpriteFont, NullMeta> FontHeaderBig = GetFont(
            40,
            "fonts/Renogare-Regular",
            "fonts/Poppins-Regular"
        );

        public static readonly Reloadable<DynamicSpriteFont, NullMeta> FontHeaderMedium = GetFont(
            30,
            "fonts/Renogare-Regular",
            "fonts/Poppins-Regular"
        );

        public static readonly Reloadable<DynamicSpriteFont, NullMeta> FontHeaderSmall = GetFont(
            20,
            "fonts/Renogare-Regular",
            "fonts/Poppins-Regular"
        );

        public static readonly Reloadable<DynamicSpriteFont, NullMeta> FontHeaderSmaller = GetFont(
            15,
            "fonts/Renogare-Regular",
            "fonts/Poppins-Regular"
        );

    }
}
