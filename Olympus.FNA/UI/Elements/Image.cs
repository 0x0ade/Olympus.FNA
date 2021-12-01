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
            new ColorFader(Color.White),
        };

        public Reloadable<Texture2D> Texture;

        public int AutoW {
            get {
                Texture2D tex = Texture;
                return (int) ((H / (float) tex.Height) * tex.Width);
            }
            set {
                W = value;
                H = AutoH;
            }
        }

        public int AutoH {
            get {
                Texture2D tex = Texture;
                return (int) ((W / (float) tex.Width) * tex.Height);
            }
            set {
                H = value;
                W = AutoW;
            }
        }

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
