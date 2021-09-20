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
    public class SplashComponent : AppComponent {

        public float Time = 1f;

        public SplashComponent(App app)
            : base(app) {

            DrawOrder = 1000001;
        }

        public override void Draw(GameTime gameTime) {
            float dt = gameTime.GetDeltaTime();

            if (Time < 0f) {
                App.Components.Remove(this);
                return;
            }

            Texture2D splash = Assets.Splash;
            float alpha = Ease.QuadInOut(Math.Max(0f, Math.Min(1f, Time)));
            Point size = Native.SplashSize;
            float scale = 1f + 2f * (1f - Ease.QuadOut(Math.Max(0f, Math.Min(1f, Time))));
            float width = size.X * scale;
            float height = size.Y * scale;

            SpriteBatch.Begin();
            SpriteBatch.Draw(
                OlympUI.Assets.White,
                new Vector2(0f, 0f),
                null,
                Native.SplashBG * alpha,
                0f, Vector2.Zero,
                new Vector2(App.Width, App.Height),
                SpriteEffects.None, 0f
            );
            SpriteBatch.Draw(
                splash,
                new Vector2(
                    (App.Width / 2) - width * 0.5f,
                    (App.Height / 2) - height * 0.5f 
                ),
                null,
                Native.SplashFG * alpha * alpha,
                0f, Vector2.Zero,
                new Vector2(
                    width / splash.Width,
                    height / splash.Height
                ),
                SpriteEffects.None, 0f
            );
            SpriteBatch.End();

            Time -= dt * 3f;
        }

    }
}
