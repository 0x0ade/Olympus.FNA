using Microsoft.Xna.Framework;
using OlympUI;
using SDL2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Olympus.NativeImpls {
    public unsafe partial class NativeNop : NativeImpl {

        public override bool CanRenderTransparentBackground => false;
        public override bool IsActive => App.IsActive;
        public override bool IsMaximized => false;
        public override Point WindowPosition {
            get => App.Window.ClientBounds.Location;
            set {
            }
        }

        public override bool? DarkModePreferred => null;
        public override bool DarkMode { get; set; } = false;

        public override Point SplashSize => default;
        public override Color SplashFG => default;
        public override Color SplashBG => default;

        public override bool BackgroundBlur {
            get => false;
            set {
            }
        }

        public override bool ReduceBackBufferResizes => false;

        public override Padding Padding => default;
        public override ClientSideDecorationMode ClientSideDecoration => ClientSideDecorationMode.None;


        public override bool IsMouseFocus => SDL.SDL_GetMouseFocus() == Game.Window.Handle;

        private bool _CaptureMouse;
        public override bool CaptureMouse {
            get => _CaptureMouse;
            set {
                if (_CaptureMouse == value)
                    return;
                _CaptureMouse = value;
                SDL.SDL_CaptureMouse(value ? SDL.SDL_bool.SDL_TRUE : SDL.SDL_bool.SDL_FALSE);
            }
        }

        public override Point MouseOffset => default;


        public NativeNop(App app)
            : base(app) {
        }

        public override void PrepareLate() {
        }

        public override Point FixWindowPositionDisplayDrag(Point pos) {
            return pos;
        }

        public override void Update(float dt) {
        }

        public override void BeginDrawRT(float dt) {
        }

        public override void EndDrawRT(float dt) {
        }

        public override void BeginDrawBB(float dt) {
        }

        public override void EndDrawBB(float dt) {
        }

    }
}
