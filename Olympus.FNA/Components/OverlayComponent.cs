using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OlympUI;
using Olympus.NativeImpls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Olympus {
    public class OverlayComponent : AppComponent {

        public float Time = 0f;

        public OverlayComponent(App app)
            : base(app) {

            DrawOrder = 1000000;
        }

        public override void Draw(GameTime gameTime) {
            float dt = gameTime.GetDeltaTime();

            if (Native.IsActive) {
                Time += dt * 4f;
                if (Time > 1f)
                    Time = 1f;
            } else {
                Time -= dt * 4f;
                if (Time < 0f)
                    Time = 0f;
            }

            float tintF = Ease.QuadInOut(Math.Max(0f, Math.Min(1f, Time)));
            float tintA = tintF * 0.3f;
            Color tint = new(tintA, tintA, tintA, tintA);

            DisplayMode dm = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;

            Texture2D overlay = Assets.Overlay.Value;
            float scale = Math.Max(
                Math.Max(dm.Width * 0.75f / overlay.Width, dm.Height * 0.75f / overlay.Height),
                Math.Max((float) App.Width / overlay.Width, (float) App.Height / overlay.Height)
            ) * 1.4f;
            int width = (int) (overlay.Width * scale);
            int height = (int) (overlay.Height * scale);

            float offs = App.Time * 0.001f % 1f;
            int x = (int) (width * offs) - (App.Window.ClientBounds.X % width);
            int y = (int) (height * offs) - (App.Window.ClientBounds.Y % height);

            SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.Default, UI.RasterizerStateCullCounterClockwiseScissoredNoMSAA);
            SpriteBatch.Draw(overlay, new Rectangle(x + width * -1, y + height * -1, width, height), tint);
            SpriteBatch.Draw(overlay, new Rectangle(x + width * -0, y + height * -1, width, height), tint);
            SpriteBatch.Draw(overlay, new Rectangle(x + width * +1, y + height * -1, width, height), tint);
            SpriteBatch.Draw(overlay, new Rectangle(x + width * -1, y + height * -0, width, height), tint);
            SpriteBatch.Draw(overlay, new Rectangle(x + width * -0, y + height * -0, width, height), tint);
            SpriteBatch.Draw(overlay, new Rectangle(x + width * +1, y + height * -0, width, height), tint);
            SpriteBatch.Draw(overlay, new Rectangle(x + width * -1, y + height * +1, width, height), tint);
            SpriteBatch.Draw(overlay, new Rectangle(x + width * -0, y + height * +1, width, height), tint);
            SpriteBatch.Draw(overlay, new Rectangle(x + width * +1, y + height * +1, width, height), tint);
            SpriteBatch.End();

#if false
            SpriteBatch.Begin();
            SpriteBatch.Draw(
                OlympUI.Assets.White,
                new Vector2(100f, 300f),
                null,
                Color.Black,
                0f, Vector2.Zero,
                new Vector2(100f, 100f),
                SpriteEffects.None, 0f
            );
            SpriteBatch.End();
#endif
        }

    }
}
