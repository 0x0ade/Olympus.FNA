using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OlympUI;
using Olympus.NativeImpls;
using SDL2;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Olympus {
    public class MetaMainScene : Scene {

        public bool Real = true;

        public override Element Generate()
            => new Group() {
                ID = "MetaMainScene",
                Layout = {
                    Layouts.Fill(),
                    Layouts.Column(false)
                },
                Children = {
                    NativeImpl.Native.ClientSideDecoration < ClientSideDecorationMode.Title ? Element.Null :
                    new Group() {
                        ID = "Title",
                        H = 48,
                        Layout = {
                            Layouts.Fill(1, 0),
                        },
                        Children = {
                            new Icon(OlympUI.Assets.GetTexture("header_olympus")) {
                                XY = new(8, 8),
                                AutoH = 32,
                            },
                            NativeImpl.Native.ClientSideDecoration < ClientSideDecorationMode.Full ? Element.Null :
                            new Group() {
                                ID = "WindowButtons",
                                Style = {
                                    { "Spacing", 2 },
                                },
                                Layout = {
                                    Layouts.Top(),
                                    Layouts.Right(),
                                    Layouts.Row(),
                                },
                                Children = {
                                    new WindowButton("minimize") {
                                        Callback = _ => {
                                            App.Instance.ManualUpdateSkip = true;
                                            SDL.SDL_MinimizeWindow(App.Instance.Window.Handle);
                                        },
                                    },
                                    new WindowButton(() => NativeImpl.Native.IsMaximized ? "fullscreen_exit" : "fullscreen_enter") {
                                        Callback = _ => {
                                            App.Instance.ManualUpdateSkip = true;
                                            if (NativeImpl.Native.IsMaximized)
                                                SDL.SDL_RestoreWindow(App.Instance.Window.Handle);
                                            else
                                                SDL.SDL_MaximizeWindow(App.Instance.Window.Handle);
                                        },
                                    },
                                    new WindowCloseButton("close") {
                                        Callback = _ => App.Instance.Exit(),
                                    },
                                }
                            }
                        }
                    },

                    new Group() {
                        ID = "MainBox",
                        Style = {
                            { "Spacing", 0 },
                        },
                        Layout = {
                            Layouts.Fill(1, 1, 0, LayoutConsts.Prev),
                            Layouts.Row(false),
                        },
                        Children = {
                            new Group() {
                                ID = "SidebarBox",
                                W = 88,
                                Layout = {
                                    Layouts.Fill(0, 1),
                                    Layouts.Grow(0, NativeImpl.Native.ClientSideDecoration < ClientSideDecorationMode.Title ? -16 : -8),
                                    Layouts.Top(NativeImpl.Native.ClientSideDecoration < ClientSideDecorationMode.Title ? 8 : 0),
                                },
                                Children = {
                                    new Group() {
                                        ID = "SidebarTop",
                                        Style = {
                                            { "Spacing", 8 },
                                        },
                                        Layout = {
                                            Layouts.Fill(1, 0),
                                            Layouts.Column(),
                                            Layouts.Top(),
                                        },
                                        Children = {
                                            new SidebarPlayButton("play_wheel", "Everest", _ => { }),
                                            new SidebarPlayButton("play", "Vanilla", _ => { }),
                                            new SidebarNavButton("everest", "Home", Scener.Get<HomeScene>()),
                                            new SidebarNavButton("gamebanana", "Find Mods", Scener.Get<TestScene>()),
                                            // new SidebarNavButton("loenn", "Lönn", Scener.Get<TestScene>()),
                                            new SidebarNavButton("ahorn", "Ahorn", new MetaMainScene() { Real = false }),
                                            new SidebarNavButton("wiki", "Wiki", Scener.Get<TestScene>()),
                                        }
                                    },
                                    new Group() {
                                        ID = "SidebarBottom",
                                        Style = {
                                            { "Spacing", 8 },
                                        },
                                        Layout = {
                                            Layouts.Fill(1, 0),
                                            Layouts.Column(),
                                            Layouts.Bottom(),
                                        },
                                        Children = {
                                            new SidebarNavButton("download", "Downloads", Scener.Get<TestScene>()),
                                            new SidebarNavButton("cogwheel", "Settings", Scener.Get<ConfigurationScene>()),
                                        }
                                    },
                                }
                            },

                            new Panel() {
                                ID = "ContentBox",
                                Style = {
                                    { "Padding", 0 },
                                },
                                Layout = {
                                    Layouts.Fill(1, 1, LayoutConsts.Prev, 0),
                                    Layouts.Grow(8, 8),
                                    Layouts.Column(false),
                                },
                                Children = {
                                    new Group() {
                                        ID = "Path",
                                        H = 48,
                                        Layout = {
                                            Layouts.Fill(1, 0, 16, 0),
                                            Layouts.Grow(-8 - NativeImpl.Native.Padding.Right * 2, 0),
                                            Layouts.Left(8 + NativeImpl.Native.Padding.Right),
                                            Layouts.Top(8 + NativeImpl.Native.Padding.Right),
                                            Layouts.Row(false),
                                        },
                                        Init = Element.Cast((Group pathBar) => {
                                            Scener.OnChange += (prev, next) => {
                                                pathBar.Children.Clear();
                                                foreach (Scene scene in Scener.Stack) {
                                                    pathBar.Children.Add(new HeaderBig(scene.Name));
                                                }
                                            };
                                        })
                                    },
                                    new Group() {
                                        ID = "ContentContainer",
                                        Cached = true,
                                        CachePadding = new() {
                                            Bottom = NativeImpl.Native.Padding.Bottom,
                                        },
                                        Clip = true,
                                        Layout = {
                                            Layouts.Fill(1, 1, 0, LayoutConsts.Prev),
                                            Layouts.Grow(-8 - NativeImpl.Native.Padding.Right * 2, -8 - NativeImpl.Native.Padding.Bottom),
                                            Layouts.Left(NativeImpl.Native.Padding.Right),
                                        },
                                        Children = {
                                            Real ? Scener.RootContainer : new Label("recursion"),
                                        }
                                    }
                                }
                            },
                        }
                    },
                }
            };

        public class SidebarButton : Button {

            public static readonly new Style DefaultStyle = new() {
                {
                    "Normal",
                    new Style() {
                        { "Background", new Color(0x00, 0x00, 0x00, 0x50) },
                        { "Shadow", 0f },
                    }
                },

                {
                    "Disabled",
                    new Style() {
                        { "Background", new Color(0x70, 0x70, 0x70, 0x70) },
                        { "Shadow", 0f },
                    }
                },

                {
                    "Hovered",
                    new Style() {
                        { "Background", new Color(0x60, 0x60, 0x60, 0x70) },
                        { "Shadow", 0f },
                    }
                },

                {
                    "Pressed",
                    new Style() {
                        { "Background", new Color(0x30, 0x30, 0x30, 0x70) },
                        { "Shadow", 0f },
                    }
                },

                { "Padding", 0 },
            };

            public SidebarButton(string icon, string text)
                : this(OlympUI.Assets.GetTexture($"icons/{icon}"), text) {
            }

            public SidebarButton(Reloadable<Texture2D> icon, string text)
                : base() {
                X = 8;
                WH = new(72, 64);

                Icon iconi = new(icon) {
                    ID = "icon",
                    Style = {
                        { "Color", Style.GetLink("Foreground") },
                    },
                    Layout = {
                        Layouts.Left(0.5f, -0.5f),
                        Layouts.Top(8),
                    }
                };
                Texture2D icont = icon;
                if (icont.Width > icont.Height) {
                    iconi.AutoW = 32;
                } else {
                    iconi.AutoH = 32;
                }
                Children.Add(iconi);

                Children.Add(new Label(text) {
                    ID = "label",
                    Y = 48,
                    Style = {
                        { "Color", Style.GetLink("Foreground") },
                        OlympUI.Assets.FontSmall,
                    },
                    Layout = {
                        Layouts.Left(0.5f, -0.5f),
                        Layouts.Bottom(4),
                    }
                });
            }

        }

        public class SidebarNavButton : SidebarButton {

            public static readonly new Style DefaultStyle = new() {
                {
                    "Current",
                    new Style() {
                        { "Background", new Color(0x00, 0x0a, 0x0d, 0x50) },
                        { "Foreground", new Color(0xff, 0xff, 0xff, 0xff) },
                        { "Shadow", 0f },
                    }
                },
            };

            public readonly Scene Scene;

            public bool Current => Scener.Stack.Contains(Scene);

            public override string StyleState =>
                Current ? "Current" :
                base.StyleState;

            public SidebarNavButton(string icon, string text, Scene scene)
                : this(OlympUI.Assets.GetTexture($"icons/{icon}"), text, scene) {
            }

            public SidebarNavButton(Reloadable<Texture2D> icon, string text, Scene scene)
                : base(icon, text) {
                Scene = scene;
                Add(new SidebarNavButtonIndicator(this) {
                    X = 4,
                    W = 4,
                    Layout = {
                        Layouts.Fill(0, 1, 0, 24 * 2),
                        Layouts.Top(0.5f, -0.5f),
                    }
                });
                Callback += b => Scener.Set(((SidebarNavButton) b).Scene);
            }

        }

        public class SidebarNavButtonIndicator : Element {

            public static readonly new Style DefaultStyle = new() {
                {
                    "Normal",
                    new Style() {
                        { new Color(0x00, 0x00, 0x00, 0x00) },
                        { "Scale", 0f },
                    }
                },

                {
                    "Active",
                    new Style() {
                        { new Color(0x00, 0xad, 0xee, 0xff) },
                        { "Scale", 1f },
                    }
                },

                new ColorFader(),
                { "Scale", new FloatFader() },
            };

            public readonly SidebarNavButton Button;

            private BasicMesh Mesh;
            private Color PrevColor;
            private float PrevScale;
            private Point PrevWH;

            public SidebarNavButtonIndicator(SidebarNavButton button) {
                MSAA = true;
                Button = button;
                Mesh = new BasicMesh(Game.GraphicsDevice) {
                    Texture = OlympUI.Assets.GradientQuad
                };
            }

            public override void Update(float dt) {
                Style.Apply(Button.Current ? "Active" : "Normal");

                base.Update(dt);
            }

            public override void DrawContent() {
                SpriteBatch.End();

                Color color = Style.GetCurrent<Color>();
                float scale = Style.GetCurrent<float>("Scale");
                Point wh = WH;

                if (PrevColor != color ||
                    PrevScale != scale ||
                    PrevWH != wh) {
                    MeshShapes shapes = Mesh.Shapes;
                    shapes.Clear();

                    if (color != default) {
                        shapes.Add(new MeshShapes.Rect() {
                            Color = color,
                            XY1 = new(0f, wh.Y * (0.5f - 0.5f * scale)),
                            Size = new(wh.X, wh.Y * scale),
                            Radius = Math.Min(wh.X, wh.Y * scale) * 0.5f,
                        });
                    }

                    // Fix UVs manually as we're using a gradient texture.
                    for (int i = 0; i < shapes.VerticesMax; i++) {
                        ref VertexPositionColorTexture vertex = ref shapes.Vertices[i];
                        vertex.TextureCoordinate = new(1f, 1f);
                    }

                    shapes.AutoApply();
                }

                Mesh.Draw(UI.CreateTransform(ScreenXY));

                SpriteBatch.BeginUI();
                base.DrawContent();

                PrevColor = color;
                PrevScale = scale;
                PrevWH = wh;
            }

        }

        public class SidebarPlayButton : SidebarButton {

            public static readonly new Style DefaultStyle = new() {
                {
                    "Normal",
                    new Style() {
                        { "Background", new Color(0x00, 0x30, 0x10, 0x50) },
                        { "Shadow", 0f },
                    }
                },

                {
                    "Disabled",
                    new Style() {
                        { "Background", new Color(0x70, 0x70, 0x70, 0x70) },
                        { "Shadow", 0f },
                    }
                },

                {
                    "Hovered",
                    new Style() {
                        { "Background", new Color(0x50, 0x70, 0x55, 0x70) },
                        { "Shadow", 0f },
                    }
                },

                {
                    "Pressed",
                    new Style() {
                        { "Background", new Color(0x30, 0x50, 0x35, 0x70) },
                        { "Shadow", 0f },
                    }
                },
            };

            public SidebarPlayButton(string icon, string text, Action<SidebarPlayButton> cb)
                : this(OlympUI.Assets.GetTexture($"icons/{icon}"), text, cb) {
            }

            public SidebarPlayButton(Reloadable<Texture2D> icon, string text, Action<SidebarPlayButton> cb)
                : base(icon, text) {
                Callback += b => cb((SidebarPlayButton) b);
            }

        }

        public class WindowButton : Button {

            public static readonly new Style DefaultStyle = new() {
                {
                    "Normal",
                    new Style() {
                        { "Background", new Color(0x00, 0x00, 0x00, 0x50) },
                        { "Shadow", 0f },
                    }
                },

                {
                    "Disabled",
                    new Style() {
                        { "Background", new Color(0x70, 0x70, 0x70, 0x70) },
                        { "Shadow", 0f },
                    }
                },

                {
                    "Hovered",
                    new Style() {
                        { "Background", new Color(0x60, 0x60, 0x60, 0x70) },
                        { "Shadow", 0f },
                    }
                },

                {
                    "Pressed",
                    new Style() {
                        { "Background", new Color(0x30, 0x30, 0x30, 0x70) },
                        { "Shadow", 0f },
                    }
                },

                { "Radius", 0f },

                { "Padding", 0 },
            };

            public Func<Reloadable<Texture2D>> IconGen;
            private Icon Icon;

            public WindowButton(string icon)
                : this(OlympUI.Assets.GetTexture($"icons/{icon}")) {
            }

            public WindowButton(Reloadable<Texture2D> icon)
                : this(() => icon) {
            }

            public WindowButton(Func<string> icon)
                : this(() => OlympUI.Assets.GetTexture($"icons/{icon()}")) {
            }

            public WindowButton(Func<Reloadable<Texture2D>> iconGen)
                : base() {
                X = 8;
                WH = new(48, 32);

                IconGen = iconGen;
                Reloadable<Texture2D> icon = iconGen();

                Icon iconi = Icon = new(icon) {
                    ID = "icon",
                    Style = {
                        { "Color", Style.GetLink("Foreground") },
                    },
                    Layout = {
                        Layouts.Left(0.5f, -0.5f),
                        Layouts.Top(0.5f, -0.5f),
                    }
                };
                Texture2D icont = icon;
                if (icont.Width > icont.Height) {
                    iconi.AutoW = 16;
                } else {
                    iconi.AutoH = 16;
                }
                Children.Add(iconi);
            }

            public override void Update(float dt) {
                Reloadable<Texture2D> next = IconGen();
                if (next != Icon.Texture) {
                    Icon.Texture = next;
                    Icon.InvalidatePaint();
                }

                base.Update(dt);
            }

        }

        public class WindowCloseButton : WindowButton {

            public static readonly new Style DefaultStyle = new() {
                {
                    "Hovered",
                    new Style() {
                        { "Background", new Color(0x90, 0x20, 0x20, 0x90) },
                    }
                },

                {
                    "Pressed",
                    new Style() {
                        { "Background", new Color(0x70, 0x10, 0x10, 0x90) },
                    }
                },
            };

            public WindowCloseButton(string icon)
                : base(icon) {
            }

            public WindowCloseButton(Reloadable<Texture2D> icon)
                : base(icon) {
            }

            public WindowCloseButton(Func<string> icon)
                : base(icon) {
            }

            public WindowCloseButton(Func<Reloadable<Texture2D>> iconGen)
                : base(iconGen) {

            }

        }

    }

}
