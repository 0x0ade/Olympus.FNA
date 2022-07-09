using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Olympus.NativeImpls;

namespace Olympus {
    public abstract class AppComponent : DrawableGameComponent {

        public readonly App App;
        public NativeImpl Native => NativeImpl.Native;
        public SpriteBatch SpriteBatch => App.SpriteBatch;

        public AppComponent(App app)
            : base(app) {
            App = app;
        }

        public abstract bool UpdateDraw();

    }
}
