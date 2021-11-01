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

        public BasicMesh? Mesh;

        public OverlayComponent(App app)
            : base(app) {

            DrawOrder = 1000000;
        }

        protected override void LoadContent() {
            Mesh = new(GraphicsDevice) {
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
                Texture = Assets.Overlay,
                BlendState = BlendState.Additive,
                SamplerState = SamplerState.LinearWrap,
            };
            Mesh.Reload();

            base.LoadContent();
        }

        public override void Draw(GameTime gameTime) {
            if (Mesh == null)
                return;

            float dt = gameTime.GetDeltaTime();

            if (Native.IsActive) {
                Time += dt * 4f;
                if (Time > 1f)
                    Time = 1f;
            } else {
                Time -= dt * 4f;
                if (Time < 0f)
                    Time = 0f;
            }

            float tintF = Ease.QuadInOut(Math.Max(0f, Math.Min(1f, Time)));
            float tintA = tintF * 0.25f;
            Color tint = new(tintA, tintA, tintA, tintA);

            DisplayMode dm = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;

            Texture2D overlay = Assets.Overlay.Value;
            float scale = Math.Max(
                Math.Max(dm.Width * 0.75f / overlay.Width, dm.Height * 0.75f / overlay.Height),
                Math.Max((float) App.Width / overlay.Width, (float) App.Height / overlay.Height)
            ) * 1.4f;
            int width = (int) (overlay.Width * scale);
            int height = (int) (overlay.Height * scale);

            float offs = App.Time * 0.001f % 1f;
            int x = (int) (width * offs) + (App.Window.ClientBounds.X % width);
            int y = (int) (height * offs) + (App.Window.ClientBounds.Y % height);
            Vector2 uvTL = new(x / (float) overlay.Width, y / (float) overlay.Height);
            Vector2 uvBR = new((x + width) / (float) overlay.Width, (y + height) / (float) overlay.Height);

            Mesh.Color = tint;
            fixed (VertexPositionColorTexture* vertices = &Mesh.Vertices[0]) {
                vertices[0].TextureCoordinate = new(uvTL.X, uvTL.Y);
                vertices[1].Position = new(App.Width, 0, 0);
                vertices[1].TextureCoordinate = new(uvBR.X, uvTL.Y);
                vertices[2].Position = new(0, App.Height, 0);
                vertices[2].TextureCoordinate = new(uvTL.X, uvBR.Y);
                vertices[3].Position = new(App.Width, App.Height, 0);
                vertices[3].TextureCoordinate = new(uvBR.X, uvBR.Y);
            }
            Mesh.QueueNext();
            Mesh.Draw();

#if false
            SpriteBatch.Begin();
            SpriteBatch.Draw(
                OlympUI.Assets.White,
                new Vector2(100f, 300f),
                null,
                Color.Black,
                0f, Vector2.Zero,
                new Vector2(100f, 100f),
                SpriteEffects.None, 0f
            );
            SpriteBatch.End();
#endif
        }

    }
}
