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
    public class MetaMainScene : Scene {

        public override Element Generate()
            => new Group() {
                ID = "MetaMainScene",
                Layout = {
                    Layouts.Fill(),
                    Layouts.Column()
                },
                Children = {
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
                        }
                    },

                    new Group() {
                        ID = "MainBox",
                        Style = {
                            { "Spacing", 8 },
                        },
                        Layout = {
                            Layouts.Fill(1, 1, 0, LayoutConsts.Prev),
                            Layouts.Row(),
                        },
                        Children = {
                            new Group() {
                                ID = "SidebarBox",
                                W = 88,
                                Layout = {
                                    Layouts.Fill(0, 1),
                                    Layouts.Grow(0, -8),
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
                                            new SidebarNavButton("update", "Updates", Scener.Get<HomeScene>()),
                                            new SidebarNavButton("everest", "Home", Scener.Get<HomeScene>()),
                                            new SidebarNavButton("gamebanana", "Find Mods", Scener.Get<HomeScene>()),
                                            new SidebarNavButton("berry", "Installed", Scener.Get<HomeScene>()),
                                            // new SidebarNavButton("loenn", "Lönn", Scener.Get<HomeScene>()),
                                            new SidebarNavButton("ahorn", "Ahorn", Scener.Get<HomeScene>()),
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
                                            new SidebarNavButton("cogwheel", "Options", Scener.Get<HomeScene>()),
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
                                    Layouts.Column(),
                                },
                                Children = {
                                    new Group() {
                                        ID = "Path",
                                        H = 48,
                                        Layout = {
                                            Layouts.Fill(1, 0, 16, 0),
                                            Layouts.Grow(-8, 0),
                                            Layouts.Left(8),
                                            Layouts.Top(8),
                                            Layouts.Row(),
                                        },
                                        Children = {

                                        },
                                        Init = Element.Cast((Group pathBar) => {
                                            Scener.OnChange += (prev, next) => {
                                                pathBar.Children.Clear();
                                                foreach (Scene scene in Scener.Stack) {
                                                    pathBar.Add(new Header(scene.Name));
                                                }
                                            };
                                        })
                                    },
                                    new Group() {
                                        ID = "ContentContainer",
                                        Cached = true,
                                        CachePadding = 0,
                                        Clip = true,
                                        Layout = {
                                            Layouts.Fill(1, 1, 0, LayoutConsts.Prev),
                                            Layouts.Grow(-8, -8),
                                        },
                                        Children = {
                                            Scener.RootContainer,
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

                Icon iconi = new Icon(icon) {
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

            public SidebarNavButton(string icon, string text, Scene scene)
                : this(OlympUI.Assets.GetTexture($"icons/{icon}"), text, scene) {
            }

            public SidebarNavButton(Reloadable<Texture2D> icon, string text, Scene scene)
                : base(icon, text) {
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

    }

}
