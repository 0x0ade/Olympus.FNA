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

        private Skin SkinDefault;
        private Skin? SkinForce;
        private Skin? SkinDark;
        private Skin? SkinLight;

        private bool DrawDebug =
#if DEBUG
            true;
#else
            false;
#endif

        public MainComponent(App app)
            : base(app) {
            SkinDefault = Skin.CreateDump();
            SkinDark = SkinDefault;
            SkinLight = Skin.CreateLight();

            UI.Initialize(App, NativeImpl.Native);
            UI.Root.Children.Add(Scener.Get<MetaMainScene>().Generate());
            Scener.Push<HomeScene>();
        }

        protected override void LoadContent() {
            UI.LoadContent();

            base.LoadContent();
        }

        public override void Update(GameTime gameTime) {
            float dt = gameTime.GetDeltaTime();

            if (UIInput.Pressed(Keys.F1)) {
                DrawDebug = !DrawDebug;
            }

            if (UIInput.Pressed(Keys.F2)) {
                if (UIInput.Down(Keys.LeftShift)) {
                    OlympUI.Assets.ReloadID++;
                } else {
                    Native.DarkMode = !Native.DarkMode;
                }
            }

            if (UIInput.Pressed(Keys.F5)) {
                string path = Path.Combine(Environment.CurrentDirectory, "skin.yaml");
                if (!File.Exists(path)) {
                    SkinForce = null;
                } else {
                    using StreamReader reader = new(new FileStream(path, FileMode.Open));
                    SkinForce = Skin.Deserialize(reader);
                }
            }

            if (UIInput.Pressed(Keys.F7)) {
                if (UIInput.Down(Keys.LeftShift)) {
                    string path = Path.Combine(Environment.CurrentDirectory, "megacanvas");
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                    UI.MegaCanvas.Dump(path);
                } else {
                    string path = Path.Combine(Environment.CurrentDirectory, "skin.yaml");
                    using StreamWriter writer = new(new FileStream(path, FileMode.Create));
                    Skin.Serialize(writer, Skin.CreateDump());
                }
            }

            if (UIInput.Pressed(Keys.F12)) {
                UI.GlobalDrawDebug = !UI.GlobalDrawDebug;
                UI.GlobalRepaintID++;
            }

            if (Skin.Current != (Skin.Current = SkinForce ?? (Native.DarkMode ? SkinDark : SkinLight))) {
                UI.GlobalRepaintID++;
            }

            // FIXME: WHY IS YET ANOTHER ROW OF PIXELS MISSING?! Is this an OlympUI bug or another Windows quirk?
            // FIXME: Size flickering in maximized mode on Windows when using viewport size for UI root size.
            // UI.Root.WH = new(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height - (Native.IsMaximized ? 8 : 1));
            UI.Root.WH = new(App.Width, App.Height - (Native.IsMaximized ? 8 : 1));

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

            if (DrawDebug) {
                string debug =
                    $"FPS: {App.FPS}\n" +
                    $"Mouse: {UIInput.Mouse}\n" +
                    $"Root Size: {UI.Root.WH.X} x {UI.Root.WH.Y}\n" +
                    $"App Size: {App.Width} x {App.Height} ({(Native.IsMaximized ? "maximized" : "windowed")})\n" +
                    $"Pool Available: {UI.MegaCanvas.PoolEntriesAlive}\n" +
                    $"Pool Used: {UI.MegaCanvas.PoolUsed.Count}\n" +
                    $"Pool Memory: {GetHumanFriendlyBytes(UI.MegaCanvas.PoolUsedMemory)} / {GetHumanFriendlyBytes(UI.MegaCanvas.PoolTotalMemory)} \n" +
                    $"Atlas Pages: {UI.MegaCanvas.Pages.Count} x {GetHumanFriendlyBytes(UI.MegaCanvas.PageSize * UI.MegaCanvas.PageSize * 4)} \n" +
                    "";
                const int debugOutline = 1;

                SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, UI.RasterizerStateCullCounterClockwiseScissoredNoMSAA);
                SpriteBatch.DrawString(OlympUI.Assets.FontMono, debug, new Vector2(debugOutline * 1, debugOutline * 0), Color.Black);
                SpriteBatch.DrawString(OlympUI.Assets.FontMono, debug, new Vector2(debugOutline * 2, debugOutline * 1), Color.Black);
                SpriteBatch.DrawString(OlympUI.Assets.FontMono, debug, new Vector2(debugOutline * 1, debugOutline * 2), Color.Black);
                SpriteBatch.DrawString(OlympUI.Assets.FontMono, debug, new Vector2(debugOutline * 0, debugOutline * 1), Color.Black);
                SpriteBatch.DrawString(OlympUI.Assets.FontMono, debug, new Vector2(debugOutline * 1, debugOutline * 1), Color.Red);
                SpriteBatch.End();
            }
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
