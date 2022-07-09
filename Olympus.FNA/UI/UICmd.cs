using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OlympUI.MegaCanvas;
using System;
using System.Collections.Generic;

namespace OlympUI {
    public static class UICmd {

        public readonly record struct Blit(Texture2D Texture, Rectangle Source, Rectangle Destination, Color Color) : IRecorderCmd {
            public void Invoke() => UI.SpriteBatch.Draw(Texture, Destination, Source, Color);
        }

    }
}
