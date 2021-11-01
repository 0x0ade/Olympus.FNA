using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OlympUI;
using Olympus.NativeImpls;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Olympus {
    public class TestScene : Scene {

        public override Element Generate()
            => new Group() {
                Layout = {
                    Layouts.Fill()
                },
                Children = {
                    new Icon(OlympUI.Assets.GetTexture("header_olympus")),
                    new Panel() {
                        X = 100,
                        Y = 100,
                        Children = {
                            new Label("a") {
                                Style = {
                                    OlympUI.Assets.FontMono
                                }
                            },
                            new Label("b") {
                                Style = {
                                    Color.Red
                                },
                                Layout = {
                                    e => {
                                        Element el = e.Element;
                                        Element prev = el.Siblings[-1];
                                        el.XY = new(prev.X + prev.W, prev.Y);
                                    }
                                }
                            }
                        }
                    },
                    new Panel() {
                        X = 300,
                        Y = 100,
                        Style = {
                            { "Background", new Color(0xf0, 0xf0, 0xf0, 0xff) },
                            { "Shadow", 2f }
                        }
                    },
                    new Panel() {
                        X = 500,
                        Y = 100,
                        Style = {
                            { "Background", Color.Black },
                            { "Shadow", 1f }
                        },
                        Events = {
                            (MouseEvent.Enter e) => e.Element.Style = new() {
                                { "Background", Color.White },
                                { "Shadow", 3f }
                            },
                            (MouseEvent.Leave e) => e.Element.Style = new() {
                                { "Background", Color.Black },
                                { "Shadow", 1f }
                            },
                        }
                    },
                    new Panel() {
                        X = 700,
                        Y = 100,
                        Style = {
                            { "Background", Color.Black },
                            { "Shadow", 1f }
                        },
                        Events = {
                            (MouseEvent.Press e) => e.Element.Style = new() {
                                { "Background", Color.White },
                                { "Shadow", 3f }
                            },
                            (MouseEvent.Release e) => e.Element.Style = new() {
                                { "Background", Color.Black },
                                { "Shadow", 1f }
                            },
                            (MouseEvent.Click e) => e.Element.Style = new() {
                                { "Background", Color.Red },
                                { "Shadow", 5f }
                            },
                            (MouseEvent.Scroll e) => e.Element.Style = new() {
                                { "Background", e.ScrollDXY.Y <= 0 ? Color.Green : Color.Blue },
                                { "Shadow", 5f }
                            },
                        }
                    },
                    new Panel() {
                        Layout = {
                            Layouts.Left(0.5f, -0.5f),
                            Layouts.Top(0.5f, -0.5f),
                            Layouts.Fill(0.5f, 0.5f),
                        },
                        Cached = false,
                        Children = {
                            new ScrollBox() {
                                Layout = {
                                    Layouts.Fill()
                                },
                                Content = new Panel() {
                                    H = 800,
                                    Layout = {
                                        Layouts.Fill(1, 0),
                                        Layouts.Column()
                                    },
                                    Children = {
                                        new Label("Test Label"),
                                        new Button("Test Button")  {
                                            Layout = { Layouts.Fill(1, 0) },
                                            Data = { 0 },
                                            Callback = el => el.Text = $"Pressed: {++el.Data.Ref<int>()}"
                                        },
                                        new Button("Toggle Clip") {
                                            Layout = { Layouts.Fill(1, 0) },
                                            Callback = el => {
                                                Element container = el.GetParent<ScrollBox>().GetParent<Element>();
                                                container.Clip = !container.Clip;
                                                container.Cached = container.Clip;
                                            }
                                        },
                                        new Button("Disabled Button") {
                                            Layout = { Layouts.Fill(1, 0) },
                                            Enabled = false
                                        },
                                        new PathTest() {
                                            H = 200,
                                            Layout = { Layouts.Fill(1, 0) },
                                        },
                                    },
                                }
                            }
                        }
                    },
                    new Panel() {
                        Layout = {
                            Layouts.Row(8),
                            Layouts.Bottom(8),
                            Layouts.Right(8),
                        },
                        Children = {
                            new Spinner() {
                                Style = { new Color(0x00, 0xad, 0xee, 0xff) }
                            },
                            new Label("bottom text") {
                                Layout = { Layouts.Top(0.5f, -0.5f) },
                                Style = { new Color(0x00, 0xad, 0xee, 0xff) }
                            }
                        }
                    },
                }
            };

        public class PathTest : Element {

            public static readonly new Style DefaultStyle = new() {
                new ColorFader(new Color(1f, 0f, 0.5f, 1f) * 0.25f)
            };

            private BasicMesh Mesh;
            private Color PrevColor;
            private Point PrevWH;

            public PathTest() {
                Mesh = new BasicMesh(Game.GraphicsDevice) {
                    Texture = OlympUI.Assets.White
                };
            }

            public override void DrawContent() {
                SpriteBatch.End();

                Color color = Style.GetCurrent<Color>();
                Point wh = WH;

                if (PrevColor != color ||
                    PrevWH != wh) {
                    MeshShapes shapes = Mesh.Shapes;
                    shapes.Clear();

                    if (color != default) {
                        shapes.Add(new MeshShapes.Poly() {
                            color,
                            25f,
                            new Vector2(W * 0f, H * 0f),
                            new Vector2(W * 1f, H * 0f),
                            new Vector2(W * 1f, H * 1f),
                            new Vector2(W * 0.45f, H * 0.75f),
                            new Vector2(W * 0.65f, H * 0.25f),
                            new Vector2(W * 0f, H * 0f),
                        });
                    }

                    shapes.AutoApply();
                }

                Mesh.WireFrame = false;
                Mesh.Draw(UI.CreateTransform(ScreenXY));
                Mesh.WireFrame = true;
                Mesh.Draw(UI.CreateTransform(ScreenXY));

                SpriteBatch.BeginUI();
                base.DrawContent();

                PrevColor = color;
                PrevWH = wh;
            }

        }

    }

}
