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
    public unsafe class OverlayComponent : AppComponent {

        public float Time = 0f;
        private bool Redraw;

        private Reloadable<Texture2D, Texture2DMeta>? Overlay;
        private BasicMesh? Mesh;

        public OverlayComponent(App app)
            : base(app) {

            DrawOrder = 1000000;
        }

        public override void Initialize() {
            base.Initialize();

            Overlay = OlympUI.Assets.GetTexturePremulUnmipped("overlay");
        }

        protected override void LoadContent() {
            if (Overlay is null)
                return;

            Mesh = new(Game) {
                Shapes = {
                    // Will be updated in Draw.
                    new MeshShapes.Quad() {
                        XY1 = new(0, 0),
                        XY2 = new(1, 0),
                        XY3 = new(0, 1),
                        XY4 = new(1, 1),
                        UV1 = new(0, 0),
                        UV2 = new(1, 0),
                        UV3 = new(0, 1),
                        UV4 = new(1, 1),
                    },
                },
                MSAA = false,
                Texture = Overlay,
                BlendState = BlendState.Additive,
                SamplerState = SamplerState.LinearWrap,
            };
            Mesh.Reload();

            base.LoadContent();
        }

        public override void Update(GameTime gameTime) {
            float dt = gameTime.GetDeltaTime();

            float prevTime = Time;

            if (Native.IsActive) {
                Time += dt * 4f;
                if (Time > 1f)
                    Time = 1f;
            } else {
                Time -= dt * 4f;
                if (Time < 0f)
                    Time = 0f;
            }

            Redraw = Time != prevTime;
        }

        public override bool UpdateDraw() {
            return Redraw;
        }

        public override void Draw(GameTime gameTime) {
            Redraw = false;

            if (Mesh is null)
                return;

            float tintF = Ease.QuadInOut(Math.Max(0f, Math.Min(1f, Time)));
            float tintA = tintF * 0.25f;
            Color tint = new(tintA, tintA, tintA, tintA);

            DisplayMode dm = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;

            Texture2D overlay = Mesh.Texture.Value;
            float scale = Math.Max(dm.Width / overlay.Width, dm.Height / overlay.Height) * 1.5f;
            float width = App.Width * scale;
            float height = App.Height * scale;

            float x = App.Window.ClientBounds.X;
            float y = App.Window.ClientBounds.Y;
            Vector2 uvTL = new(x / overlay.Width, y / overlay.Height);
            Vector2 uvBR = new((x + width) / overlay.Width, (y + height) / overlay.Height);

            Mesh.Color = tint;
            fixed (MiniVertex* vertices = &Mesh.Vertices[0]) {
                vertices[0].UV = new(uvTL.X, uvTL.Y);
                vertices[1].XY = new(App.Width, 0);
                vertices[1].UV = new(uvBR.X, uvTL.Y);
                vertices[2].XY = new(0, App.Height);
                vertices[2].UV = new(uvTL.X, uvBR.Y);
                vertices[3].XY = new(App.Width, App.Height);
                vertices[3].UV = new(uvBR.X, uvBR.Y);
            }
            Mesh.QueueNext();
            Mesh.Draw();

#if false
            SpriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullCounterClockwise,
                null,
                Matrix.Identity
            );
            SpriteBatch.Draw(
                OlympUI.Assets.Test,
                new Vector2(100f, 100f),
                null,
                Color.White,
                0f, Vector2.Zero,
                new Vector2(1f, 1f),
                SpriteEffects.None, 0f
            );
            SpriteBatch.End();
#endif
        }

    }
}
