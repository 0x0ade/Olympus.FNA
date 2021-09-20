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
    public class Button : Panel {

        public static readonly new Style DefaultStyle = new() {
            {
                "Normal",
                new Style() {
                    { "Background", new Color(0x30, 0x30, 0x30, 0xff) },
                    { "Foreground", new Color(0xff, 0xff, 0xff, 0xff) },
                    { "Shadow", 1f },
                }
            },

            {
                "Disabled",
                new Style() {
                    { "Background", new Color(0x70, 0x70, 0x70, 0xff) },
                    { "Foreground", new Color(0x30, 0x30, 0x30, 0xff) },
                    { "Shadow", 0.75f },
                }
            },

            {
                "Hovered",
                new Style() {
                    { "Background", new Color(0x40, 0x40, 0x40, 0xff) },
                    { "Foreground", new Color(0xff, 0xff, 0xff, 0xff) },
                    { "Shadow", 2f },
                }
            },

            {
                "Pressed",
                new Style() {
                    { "Background", new Color(0x20, 0x20, 0x20, 0xff) },
                    { "Foreground", new Color(0xff, 0xff, 0xff, 0xff) },
                    { "Shadow", 0.5f },
                }
            },

            { "Foreground", new ColorFader() },
        };

        public bool Enabled = true;
        public Action<Button>? Callback;

        public string Text {
            get => GetChild<Label>()?.Text ?? "";
            set => _ = GetChild<Label>() is Label label ? label.Text = value : null;
        }

        public Button() {
            Interactive = InteractiveMode.Process;
        }

        public Button(string text)
            : this() {
            Layout.Add(Layouts.Row());

            Children.Add(new Label(text) {
                ID = "label",
                Style = {
                    { "Color", Style.GetLink("Foreground") }
                }
            });
        }

        public Button(Action<Button> cb)
            : this() {
            Callback = cb;
        }

        public Button(string text, Action<Button> cb)
            : this(text) {
            Callback = cb;
        }

        public override void Update(float dt) {
            Style.Apply(
                !Enabled ? "Disabled" :
                Pressed ? "Pressed" :
                Hovered ? "Hovered" :
                "Normal"
            );

            base.Update(dt);
        }

        private void OnClick(MouseEvent.Click e) {
            if (Enabled)
                Callback?.Invoke(this);
        }

    }
}
