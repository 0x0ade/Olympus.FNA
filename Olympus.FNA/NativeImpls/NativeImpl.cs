using Microsoft.Xna.Framework;
using OlympUI;
using Olympus.NativeImpls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Olympus.NativeImpls {
    public abstract class NativeImpl : UINativeImpl {

#pragma warning disable CS8618 // Nothing should ever access this too early.
        public static NativeImpl Native;
#pragma warning restore CS8618

        public readonly App App;

        public abstract bool CanRenderTransparentBackground { get; }
        public abstract bool IsActive { get; }
        public abstract bool IsMaximized { get; }
        public abstract Point WindowPosition { get; set; }

        public abstract bool? DarkModePreferred { get; }
        public abstract bool DarkMode { get; set; }

        public abstract Point SplashSize { get; }
        public abstract Color SplashFG { get; }
        public abstract Color SplashBG { get; }

        public abstract bool BackgroundBlur { get; set; }

        public abstract bool ReduceBackBufferResizes { get; }

        public abstract Padding Padding { get; }
        public abstract ClientSideDecorationMode ClientSideDecoration { get; }


        public NativeImpl(App app)
            : base(app) {
            App = app;
        }

        public abstract void PrepareLate();

        public abstract Point FixWindowPositionDisplayDrag(Point pos);

        public abstract void Update(float dt);

        public abstract void BeginDrawRT(float dt);
        public abstract void EndDrawRT(float dt);

        public abstract void BeginDrawBB(float dt);
        public abstract void EndDrawBB(float dt);

    }

    public enum ClientSideDecorationMode {
        None,
        Title,
        Full
    }
}
