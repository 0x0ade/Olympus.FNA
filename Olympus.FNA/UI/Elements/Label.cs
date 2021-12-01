using FontStashSharp;
using Microsoft.Xna.Framework;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlympUI {
    public class Label : Element {

        public static readonly new Style DefaultStyle = new() {
            new ColorFader(Color.White),
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
            // FIXME: FontStashSharp can't even do basic font maximum size precomputations...

            DynamicSpriteFont font = Style.GetCurrent<DynamicSpriteFont>();
            Bounds bounds = new();
            font.TextBounds(_Text, new(0f, 0f), ref bounds, new(1f, 1f));
            WH = new((int) MathF.Round(bounds.X2), (int) MathF.Round(bounds.Y2));

            DynamicData fontExtra = new(font);
            if (!fontExtra.TryGet("MaxHeight", out int? maxHeight)) {
                font.TextBounds("The quick brown fox jumps over the lazy dog.", new(0f, 0f), ref bounds, new(1f, 1f));
                maxHeight = (int) MathF.Round(bounds.Y2);
                fontExtra.Set("MaxHeight", maxHeight);
            }

            WH.Y = Math.Max(WH.Y, maxHeight ?? 0);
        }

    }
}
