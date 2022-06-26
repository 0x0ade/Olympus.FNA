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
    public partial class MetaAlertScene : Scene {

        public override Element Generate()
            => new AlertContainer() {
                ID = "MetaAlertScene",
                Layout = {
                    Layouts.Fill(1, 1, 0, 0),
                    Layouts.Grow(0, NativeImpl.Native.ClientSideDecoration < ClientSideDecorationMode.Title ? 0 : (-48 + 8)),
                    Layouts.Top(NativeImpl.Native.ClientSideDecoration < ClientSideDecorationMode.Title ? 0 : 48),
                },
                Style = {
                    { "Padding", new Padding() {
                        L = 64,
                        T = 64,
                        R = 64,
                        B = NativeImpl.Native.ClientSideDecoration < ClientSideDecorationMode.Title ? 64 : (64 + 8)
                    } },
                    { "Radius", NativeImpl.Native.ClientSideDecoration < ClientSideDecorationMode.Title ? 0f : 8f },
                },
                Children = {
                    new Group() {
                        ID = "ContentContainer",
                        Cached = false,
                        Clip = false,
                        Interactive = InteractiveMode.Process,
                        Layout = {
                            Layouts.Fill(),
                        },
                        Children = {
                            Scener.AlertContainer,
                        }
                    },
                    new CloseButton("close") {
                        Callback = btn => {
                            if (!(Scener.Alert?.Locked ?? false)) {
                                Scener.PopAlert();
                            }
                        },
                        Layout = {
                            Layouts.Top(-8),
                            Layouts.Right(-8),
                        }
                    },
                }
            };

        public partial class AlertContainer : Panel {

            public static readonly new Style DefaultStyle = new() {
                {
                    "Disabled",
                    new Style() {
                        { "Opacity", 0f },
                        { "Shadow", 0f },
                    }
                },

                {
                    "Enabled",
                    new Style() {
                        { "Opacity", 1f },
                        { "Shadow", 4f },
                    }
                },

                { "Background", new ColorFader(0x10, 0x10, 0x10, 0xd0) },
                { "Opacity", new FloatFader(0f) },
            };

            private BlurryGroup? MainBox;

            protected Style.Entry StyleOpacity;

            public AlertContainer() {
                Style.GetEntry(out StyleOpacity);

                Cached = true;
                Clip = true;
                Interactive = InteractiveMode.Process;
            }

            public override void Awake() {
                base.Awake();

                Scener.AlertContainer.Cached = false;
                Scener.AlertContainer.Clip = false;
            }

            public override void Update(float dt) {
                BlurryGroup mainBox = MainBox ??= UI.Root.FindChild<BlurryGroup>("MainBox");

                bool enabled = Scener.Alerts.Count != 0;

                Style.Apply(enabled ? "Enabled" : "Disabled");
                mainBox.Style.Add("Radius", enabled ? 32f : 0f);
                mainBox.Style.Add("Strength", enabled ? 2f : 1f);
                mainBox.Style.Add("Scale", enabled ? 6f : 1f);
                mainBox.Style.Add("Noise", enabled ? 1f : 0f);

                InteractiveMode mode = enabled ? InteractiveMode.Process : InteractiveMode.Discard;
                if (Interactive != mode) {
                    Interactive = mode;
                    UI.Root.InvalidateCollect();
                }

                base.Update(dt);
            }

            protected override void PaintContent(bool paintToCache, bool paintToScreen, Padding padding) {
                if (StyleOpacity.GetCurrent<float>() < 0.004f) {
                    return;
                }

                base.PaintContent(paintToCache, paintToScreen, padding);
            }

            protected override void DrawCachedTexture(SpriteBatch spriteBatch, RenderTarget2D rt, Vector2 xy, Padding padding, Rectangle region) {
                spriteBatch.Draw(
                    rt,
                    new Rectangle(
                        (int) xy.X - padding.Left,
                        (int) xy.Y - padding.Top,
                        region.Width,
                        region.Height
                    ),
                    region,
                    Color.White * StyleOpacity.GetCurrent<float>()
                );
            }

            private void OnClick(MouseEvent.Click e) {
                foreach (Element child in Children)
                    if (child.Contains(e.XY))
                        return;

                if (!(Scener.Alert?.Locked ?? false)) {
                    Scener.PopAlert();
                }
            }

        }

        public partial class CloseButton : Button {

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

                { "Radius", 32f },

                { "Padding", 0 },
            };

            public Func<IReloadable<Texture2D, Texture2DMeta>> IconGen;
            private Icon Icon;

            public CloseButton(string icon)
                : this(OlympUI.Assets.GetTexture($"icons/{icon}")) {
            }

            public CloseButton(IReloadable<Texture2D, Texture2DMeta> icon)
                : this(() => icon) {
            }

            public CloseButton(Func<string> icon)
                : this(() => OlympUI.Assets.GetTexture($"icons/{icon()}")) {
            }

            public CloseButton(Func<IReloadable<Texture2D, Texture2DMeta>> iconGen)
                : base() {
                WH = new(48, 48);

                IconGen = iconGen;
                IReloadable<Texture2D, Texture2DMeta> icon = iconGen();

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
                Texture2DMeta icont = icon.Meta;
                if (icont.Width > icont.Height) {
                    iconi.AutoW = 24;
                } else {
                    iconi.AutoH = 24;
                }
                Children.Add(iconi);
            }

            public override void Update(float dt) {
                IReloadable<Texture2D, Texture2DMeta> next = IconGen();
                if (next != Icon.Texture) {
                    Icon.Texture = next;
                    Icon.InvalidatePaint();
                }

                Visible = !(Scener.Alert?.Locked ?? false);

                base.Update(dt);
            }

        }

    }
}
