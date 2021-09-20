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

        public static readonly Reloadable<Texture2D> Overlay = GetTexture("overlay");

        public static readonly Reloadable<Texture2D> Splash = GetTexture("splash");

    }
}
