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
    public class CodeWarmupComponent : AppComponent {

        public CodeWarmupComponent(App app)
            : base(app) {
        }

        public override void Initialize() {
            base.Initialize();

            // Warm up certain things separately. This helps with multi-threaded init.

            // CreateDevice can wait for a while when FNA asks SDL2 for all adapters.
            GraphicsAdapter.DefaultAdapter.IsProfileSupported(0);

            if (UI.Game is null) {
                // Trying to run Olympus without the main component, let's initialize UI ourselves.
                UI.Initialize(App, Native, App);
            }

            // The first UI input update is chonky.
            UIInput.Update();

            // The first UI update is very chonky with forced relayouts, element inits, scans and whatnot.
            UI.Update(0f);
        }

        public override void Draw(GameTime gameTime) {
            App.Components.Remove(this);
        }

    }
}
