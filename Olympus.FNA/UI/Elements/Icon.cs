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
    public class Icon : Image {

        public static readonly new Style DefaultStyle = new() {
            Label.DefaultStyle.GetLink<Color>(),
        };

        public Icon(Reloadable<Texture2D> texture)
            : base(texture) {
        }

    }
}
