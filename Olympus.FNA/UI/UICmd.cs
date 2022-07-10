using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace OlympUI {
    public static class UICmd {

        public readonly record struct Blit(Texture2D Texture, Rectangle Source, Rectangle Destination, Color Color) : IRecorderCmd {
            public void Invoke() => UI.SpriteBatch.Draw(Texture, Destination, Source, Color);
        }

        public readonly record struct DebugRect(Color Color, Rectangle Rectangle) : IRecorderCmd {
            public void Invoke() => UI.SpriteBatch.DrawDebugRect(Color, Rectangle);
        }

        public readonly record struct Text(DynamicSpriteFont Font, string Value, Vector2 Position, Color Color) : IRecorderCmd {
            public void Invoke() => UI.SpriteBatch.DrawString(Font, Value, Position, Color);
        }

    }
}
