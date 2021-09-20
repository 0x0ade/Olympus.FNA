using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
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
    public class MainComponent : AppComponent {

        public MainComponent(App app)
            : base(app) {
        }

        public override void Initialize() {
            UI.Initialize(App, NativeImpl.Native);
            UI.Root.Children.Add(Scener.RootContainer);

            base.Initialize();

            Scener.Push<TestScene>();
        }

        public override void Update(GameTime gameTime) {
            float dt = gameTime.GetDeltaTime();

            if (UIInput.Pressed(Keys.F1)) {
                if (UIInput.Down(Keys.LeftShift)) {
                    OlympUI.Assets.ReloadID++;
                } else {
                    Native.DarkMode = !Native.DarkMode;
                }
            }

            Viewport view = Game.GraphicsDevice.Viewport;
            // FIXME: WHY IS YET ANOTHER ROW OF PIXELS MISSING?! Is this an OlympUI bug or another Windows quirk?
            UI.Root.WH = new(view.Width, view.Height - (Native.IsMaximized ? 8 : 1));

            Scener.Update(dt);
            UI.Update(dt);
        }

        public override void Draw(GameTime gameTime) {
#if false
            SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullCounterClockwise);

            SpriteBatch.DrawString(Data.FontMono, $"{App.Window.ClientBounds.Width} x {App.Window.ClientBounds.Height}\n{GraphicsDevice.PresentationParameters.BackBufferWidth} x {GraphicsDevice.PresentationParameters.BackBufferHeight}\n{App.FPS} FPS", new Vector2(0, 0), Color.White);

            SpriteBatch.DrawString(Data.Font, " / / / /\n / / / /\n / / / /", new Vector2(0, 80), Color.Red);
            SpriteBatch.DrawString(Data.Font, "/ / / / \n/ / / / \n/ / / / ", new Vector2(4, 80), Color.Blue);
            SpriteBatch.DrawString(Data.Font, "The quick brown\nfox jumps over\nthe lazy dog", new Vector2(160, 80), Color.White);

#if false
            const int cornerSize = 8;
            SpriteBatch.Draw(Assets.White, new Rectangle(                           0,                             0, cornerSize, cornerSize), App.IsActive ? Color.Green : Color.Red);
            SpriteBatch.Draw(Assets.White, new Rectangle((int) App.Width - cornerSize,                             0, cornerSize, cornerSize), Native.IsActive ? Color.Green : Color.Red);
            SpriteBatch.Draw(Assets.White, new Rectangle(                           0, (int) App.Height - cornerSize, cornerSize, cornerSize), Color.Red);
            SpriteBatch.Draw(Assets.White, new Rectangle((int) App.Width - cornerSize, (int) App.Height - cornerSize, cornerSize, cornerSize), Color.Red);
#endif

            SpriteBatch.End();
#endif

            UI.Paint();
            Scener.Draw();

            string debug =
                $"FPS: {App.FPS}\n" +
                $"Mouse: {UIInput.Mouse}\n" +
                $"Root Size: {UI.Root.WH}\n" +
                $"Pool Available: {UI.MegaCanvas.PoolEntriesAlive}\n" +
                $"Pool Used: {UI.MegaCanvas.PoolUsed.Count}\n" +
                $"Pool Memory: {GetHumanFriendlyBytes(UI.MegaCanvas.PoolUsedMemory)} / {GetHumanFriendlyBytes(UI.MegaCanvas.PoolTotalMemory)} \n" +
                "";
            const int debugOutline = 1;

            SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullCounterClockwise);
            SpriteBatch.DrawString(OlympUI.Assets.FontMono, debug, new Vector2(debugOutline * 1, debugOutline * 0), Color.Black);
            SpriteBatch.DrawString(OlympUI.Assets.FontMono, debug, new Vector2(debugOutline * 2, debugOutline * 1), Color.Black);
            SpriteBatch.DrawString(OlympUI.Assets.FontMono, debug, new Vector2(debugOutline * 1, debugOutline * 2), Color.Black);
            SpriteBatch.DrawString(OlympUI.Assets.FontMono, debug, new Vector2(debugOutline * 0, debugOutline * 1), Color.Black);
            SpriteBatch.DrawString(OlympUI.Assets.FontMono, debug, new Vector2(debugOutline * 1, debugOutline * 1), Color.Red);
            SpriteBatch.End();
        }

        private static string GetHumanFriendlyBytes(long bytes) {
            return bytes switch {
                >= 1024 * 1024 * 1024 => $"{bytes / (1024f * 1024f * 1024f):N3} GB",
                >= 1024 * 1024 => $"{bytes / (1024f * 1024f):N3} MB",
                >= 1024 => $"{bytes / 1024f:N3} KB",
                _ => $"{bytes} B",
            };
        }

    }
}
