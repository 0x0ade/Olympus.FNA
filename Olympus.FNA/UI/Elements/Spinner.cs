using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlympUI {
    public class Spinner : Element {

        public static readonly new Style DefaultStyle = new() {
            Color.White,
        };

        public float Progress = -1f;

        private BasicMesh Mesh;

        private float Time;

        public Spinner() {
            Cached = false;
            WH = new(32, 32);
            Mesh = new BasicMesh(Game.GraphicsDevice) {
                Texture = Assets.White
            };
        }

        public override void Update(float dt) {
            Time = (Time + dt * 0.5f) % 1f;
            InvalidatePaint();
            base.Update(dt);
        }

        public override void DrawContent() {
            SpriteBatch.End();

            Point wh = WH;

            MeshShapes shapes = Mesh.Shapes;
            shapes.Clear();

            Color color = Style.GetCurrent<Color>();
            float radius = Math.Min(wh.X, wh.Y) * 0.5f;
            float width = radius * 0.25f;

            Vector2 c = wh.ToVector2() * 0.5f;
            radius -= width;

            const int edges = 32;

            float progA = 0f;
            float progB = Progress;

            if (progB >= 0f) {
                progB *= edges;

            } else {
                float t = Time;
                float offs = edges * t * 2f;
                if (t < 0.5f) {
                    progA = offs + 0f;
                    progB = offs + edges * t * 2f;
                } else {
                    progA = offs + edges * (t - 0.5f) * 2f;
                    progB = offs + edges;
                }
            }

            int progAE = (int) MathF.Floor(progA);
            int progBE = (int) MathF.Ceiling(progB - 1);

            if (progBE - progAE >= 1) {
                MeshShapes.Poly poly = new() {
                    Color = color,
                    Width = width,
                    UVXYMin = new(1, 1),
                    UVXYMax = new(1, 1)
                };

                for (int edge = progAE; edge <= progBE; edge++) {
                    float f = edge;

                    if (edge == progAE) {
                        f = progA;
                    } else if (edge == progBE) {
                        f = progB;
                    }

                    f = (1f - f / edges + 0.5f) * MathF.PI * 2f;
                    poly.Add(new Vector2(
                        c.X + MathF.Sin(f) * radius,
                        c.Y + MathF.Cos(f) * radius
                    ));
                }

                shapes.Add(poly);
            }

            shapes.AutoApply();

            Mesh.Draw(UI.CreateTransform(ScreenXY));

            SpriteBatch.BeginUI();
            base.DrawContent();
        }

    }
}
