using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlympUI {
    public class Image : Element {

        public static readonly new Style DefaultStyle = new() {
            Color.White,
        };

        public Reloadable<Texture2D> Texture;

        public Image(Reloadable<Texture2D> texture) {
            Texture = texture;
            Texture2D tex = texture;
            WH = new Point(tex.Width, tex.Height);
        }

        public override void DrawContent() {
            SpriteBatch.Draw(Texture, ScreenXYWH, Style.GetCurrent<Color>());
        }

    }
}
