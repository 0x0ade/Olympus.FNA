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
    public partial class Spinner : Element {

        protected Style.Entry StyleColor = new(new ColorFader(Color.White));

        public float Progress = -1f;

        private BasicMesh Mesh;

        private float Time;
        private float TimeOffs;

        protected override bool IsComposited => false;

        public Spinner() {
            WH = new(24, 24);
            Mesh = new BasicMesh(Game) {
                Texture = Assets.White
            };
        }

        public override void Update(float dt) {
            Time = (Time + dt * 0.5f) % 1f;
            TimeOffs = (TimeOffs + dt * 0.1f) % 1f;
            InvalidatePaint();
            base.Update(dt);
        }

        public override void DrawContent() {
            SpriteBatch.End();

            Point wh = WH;

            MeshShapes<MiniVertex> shapes = Mesh.Shapes;
            shapes.Clear();

            StyleColor.GetCurrent(out Color color);
            float radius = Math.Min(wh.X, wh.Y) * 0.5f;
            float width = radius * 0.25f;

            Vector2 c = wh.ToVector2() * 0.5f;
            radius -= width;

            const int edges = 64;

            float progA = 0f;
            float progB = Progress;
            float timeOffs = 0;

            if (progB >= 0f) {
                progB *= edges;

            } else {
                float t = Time;
                float offs = edges * t * 2f;
                timeOffs = TimeOffs;
                if (t < 0.5f) {
                    progA = offs + 0f;
                    progB = offs + edges * t * 2f;
                } else {
                    progA = offs + edges * (t - 0.5f) * 2f;
                    progB = offs + edges;
                }
            }

            int progAE = (int) MathF.Floor(progA);
            int progBE = (int) MathF.Ceiling(progB);

            if (progBE - progAE >= 1) {
                MeshShapes<MiniVertex>.Poly poly = new() {
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

                    f = (1f - f / edges + 0.5f + timeOffs) * MathF.PI * 2f;
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
