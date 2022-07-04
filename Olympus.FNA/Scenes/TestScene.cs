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
    public partial class TestScene : Scene {

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
                            { Panel.StyleKeys.Background, new Color(0xf0, 0xf0, 0xf0, 0xff) },
                            { Panel.StyleKeys.Shadow, 2f }
                        }
                    },
                    new Panel() {
                        X = 500,
                        Y = 100,
                        Style = {
                            { Panel.StyleKeys.Background, Color.Black },
                            { Panel.StyleKeys.Shadow, 1f }
                        },
                        Events = {
                            (MouseEvent.Enter e) => e.Element.Style = new() {
                                { Panel.StyleKeys.Background, Color.White },
                                { Panel.StyleKeys.Shadow, 3f }
                            },
                            (MouseEvent.Leave e) => e.Element.Style = new() {
                                { Panel.StyleKeys.Background, Color.Black },
                                { Panel.StyleKeys.Shadow, 1f }
                            },
                        }
                    },
                    new Panel() {
                        X = 700,
                        Y = 100,
                        Style = {
                            { Panel.StyleKeys.Background, Color.Black },
                            { Panel.StyleKeys.Shadow, 1f }
                        },
                        Events = {
                            (MouseEvent.Press e) => e.Element.Style = new() {
                                { Panel.StyleKeys.Background, Color.White },
                                { Panel.StyleKeys.Shadow, 3f }
                            },
                            (MouseEvent.Release e) => e.Element.Style = new() {
                                { Panel.StyleKeys.Background, Color.Black },
                                { Panel.StyleKeys.Shadow, 1f }
                            },
                            (MouseEvent.Click e) => e.Element.Style = new() {
                                { Panel.StyleKeys.Background, Color.Red },
                                { Panel.StyleKeys.Shadow, 5f }
                            },
                            (MouseEvent.Scroll e) => e.Element.Style = new() {
                                { Panel.StyleKeys.Background, e.ScrollDXY.Y <= 0 ? Color.Green : Color.Blue },
                                { Panel.StyleKeys.Shadow, 5f }
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
                                Style = { () => NativeImpl.Native.Accent }
                            },
                            new Label("bottom text") {
                                Layout = { Layouts.Top(0.5f, -0.5f) },
                                Style = { () => NativeImpl.Native.Accent }
                            }
                        }
                    },
                }
            };

        public partial class PathTest : Element {

            protected Style.Entry StyleColor = new(new ColorFader(() => NativeImpl.Native.Accent * 0.5f));

            private BasicMesh Mesh;
            private Color PrevColor;
            private Point PrevWH;

            protected override bool IsComposited => false;

            public PathTest() {
                Cached = false;
                Mesh = new BasicMesh(Game) {
                    Texture = OlympUI.Assets.White
                };
            }

            public override void DrawContent() {
                SpriteBatch.End();

                Style.GetCurrent(out Color color);
                Point wh = WH;

                if (PrevColor != color ||
                    PrevWH != wh) {
                    MeshShapes<MiniVertex> shapes = Mesh.Shapes;
                    shapes.Clear();

                    if (color != default) {
                        shapes.Add(new MeshShapes<MiniVertex>.Poly() {
                            color,
                            25f,
                            new Vector2(W * 0f, H * 0f),
                            new Vector2(W * 1f, H * 0f),
                            new Vector2(W * 1f, H * 1f),
                            new Vector2(W * 0.45f, H * 0.75f),
                            new Vector2(W * 0.65f, H * 0.25f),
                            new Vector2(W * 0f, H * 0f),
                        });

                        shapes.Add(new MeshShapes.Rect() {
                            Color = color,
                            XY1 = new(10f, 30f),
                            Size = new(50f, 50f),
                            Radius = 50f,
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
