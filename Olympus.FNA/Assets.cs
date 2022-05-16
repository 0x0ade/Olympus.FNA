using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OlympUI;
using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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
