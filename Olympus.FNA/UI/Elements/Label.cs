using FontStashSharp;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlympUI {
    public class Label : Element {

        public static readonly new Style DefaultStyle = new() {
            Color.White,
            Assets.Font,
        };

        private string _Text;
        public string Text {
            get => _Text;
            [MemberNotNull(nameof(_Text))]
            set {
                if (value == null)
                    value = "";
                if (_Text == value)
                    return;
                _Text = value;
                InvalidateFull();
            }
        }

        public Label(string text) {
            Text = text;
        }

        public override void DrawContent() {
            SpriteBatch.DrawString(Style.GetCurrent<DynamicSpriteFont>(), _Text, ScreenXY, Style.GetCurrent<Color>());
        }

        private void LayoutNormal(LayoutEvent e) {
            WH = Style.GetCurrent<DynamicSpriteFont>().MeasureString(_Text).ToPointRound();
        }

    }
}
