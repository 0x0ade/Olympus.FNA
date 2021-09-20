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
    public class AppComponent : DrawableGameComponent {

        public readonly App App;
        public NativeImpl Native => NativeImpl.Native;
        public SpriteBatch SpriteBatch => App.SpriteBatch;

        public AppComponent(App app)
            : base(app) {
            App = app;
        }

    }
}
