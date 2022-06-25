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
    public partial class Button : Panel {

        public static readonly new Style DefaultStyle = new() {
            {
                "Normal",
                new Style() {
                    { "Background", new Color(0x30, 0x30, 0x30, 0xff) },
                    { "Foreground", new Color(0xe8, 0xe8, 0xe8, 0xff) },
                    { "Border", new Color(0x38, 0x38, 0x38, 0x80) },
                    { "Shadow", 0.5f },
                }
            },

            {
                "Disabled",
                new Style() {
                    { "Background", new Color(0x70, 0x70, 0x70, 0xff) },
                    { "Foreground", new Color(0x30, 0x30, 0x30, 0xff) },
                    { "Border", new Color(0x28, 0x28, 0x28, 0x70) },
                    { "Shadow", 0.2f },
                }
            },

            {
                "Hovered",
                new Style() {
                    { "Background", new Color(0x50, 0x50, 0x50, 0xff) },
                    { "Foreground", new Color(0xff, 0xff, 0xff, 0xff) },
                    { "Border", new Color(0x68, 0x68, 0x68, 0x90) },
                    { "Shadow", 0.8f },
                }
            },

            {
                "Pressed",
                new Style() {
                    { "Background", new Color(0x38, 0x38, 0x38, 0xff) },
                    { "Foreground", new Color(0xff, 0xff, 0xff, 0xff) },
                    { "Border", new Color(0x18, 0x18, 0x18, 0x90) },
                    { "Shadow", 0.35f },
                }
            },

            { "Foreground", new ColorFader() },

            { "BorderSize", 1f },
            { "Radius", 4f },
        };

        public bool Enabled = true;
        public Action<Button>? Callback;

        public string Text {
            get => GetChild<Label>()?.Text ?? "";
            set => _ = GetChild<Label>() is Label label ? label.Text = value : null;
        }

        public virtual string StyleState =>
            !Enabled ? "Disabled" :
            Pressed ? "Pressed" :
            Hovered ? "Hovered" :
            "Normal";

        public Button() {
            Interactive = InteractiveMode.Process;
        }

        public Button(string text)
            : this() {
            Layout.Add(Layouts.Row(false));

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
            Style.Apply(StyleState);

            base.Update(dt);
        }

        private void OnClick(MouseEvent.Click e) {
            if (Enabled)
                Callback?.Invoke(this);
        }

    }
}
