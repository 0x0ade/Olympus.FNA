﻿using Microsoft.Xna.Framework;
using OlympUI;
using System;

namespace Olympus.NativeImpls {
    public abstract class NativeImpl : UINativeImpl, IDisposable {

#pragma warning disable CS8618 // Nothing should ever access this too early.
        public static NativeImpl Native;
#pragma warning restore CS8618

        private App? _App;
        public App App {
            get => _App ?? throw new Exception($"NativeImpl {GetType().Name} forgot to set App!");
            protected set => _App = value;
        }

        public override Game Game => App;

        public abstract bool CanRenderTransparentBackground { get; }
        public abstract bool IsActive { get; }
        public abstract bool IsMaximized { get; }
        public abstract Point WindowPosition { get; set; }

        public abstract bool? DarkModePreferred { get; }
        public abstract bool DarkMode { get; set; }

        public abstract Color Accent { get; }

        public abstract Point SplashSize { get; }
        public abstract Color SplashColorFG { get; }
        public abstract Color SplashColorBG { get; }

        public abstract bool BackgroundBlur { get; set; }

        public abstract bool ReduceBackBufferResizes { get; }

        public abstract Padding Padding { get; }
        public abstract ClientSideDecorationMode ClientSideDecoration { get; }

        public abstract bool IsMultiThreadInit { get; }


        public abstract void Run();
        public abstract void Dispose();

        public abstract void PrepareEarly();
        public abstract void PrepareLate();

        public abstract Point FixWindowPositionDisplayDrag(Point pos);

        public abstract void Update(float dt);

        public abstract void BeginDrawRT(float dt);
        public abstract void EndDrawRT(float dt);

        public abstract void BeginDrawBB(float dt);
        public abstract void EndDrawBB(float dt);

        public abstract void BeginDrawDirect(float dt);
        public abstract void EndDrawDirect(float dt);

    }

    public enum ClientSideDecorationMode {
        None,
        Title,
        Full
    }
}
