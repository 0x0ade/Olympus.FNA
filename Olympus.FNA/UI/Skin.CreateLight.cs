using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using OlympUI.MegaCanvas;
using SDL2;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;

namespace OlympUI {
    public partial class Skin {

        public static Skin CreateLight() {
            Color Invert(Color c) {
                float a = c.A / 255f;
                if (a == 0f)
                    return c;
                float aReal = a;
                float r = (c.R / 255f) / a;
                float g = (c.G / 255f) / a;
                float b = (c.B / 255f) / a;
                // return new(a - r * a, a - g * a, a - b * a, aReal);
                ColorToHSV(new(r, g, b, 1f), out float h1, out float s1, out float v1);
                ColorToHSV(new(1f - r, 1f - g, 1f - b, 1f), out float h2, out float s2, out float v2);
                Color cNew = ColorFromHSV(h1, s2, v2);
                return new((cNew.R / 255f) * a, (cNew.G / 255f) * a, (cNew.B / 255f) * a, a);
            }

            void Convert(Dictionary<string, object> props) {
                foreach (KeyValuePair<string, object> kvp in props.ToArray()) {
                    object value = kvp.Value;

                    {
                        if (value is Color c) {
                            props[kvp.Key] = value = c = Invert(c);
                        }
                    }

                    if (value is FaderStub fader) {
                        if (fader.Value is Color c) {
                            fader.Value = c = Invert(c);
                        }
                    }

                    if (value is Dictionary<string, object> dict) {
                        Convert(dict);
                    }
                }

                if (props.TryGetValue("Hovered", out object? hoveredRaw) && hoveredRaw is Dictionary<string, object> hovered &&
                    props.TryGetValue("Pressed", out object? pressedRaw) && pressedRaw is Dictionary<string, object> pressed) {
                    Dictionary<string, object> swapped = new();
                    foreach (KeyValuePair<string, object> kvp in hovered) {
                        if (kvp.Value is Color c && pressed.TryGetValue(kvp.Key, out object? cNew)) {
                            swapped[kvp.Key] = cNew;
                        } else {
                            swapped[kvp.Key] = kvp.Value;
                        }
                    }
                    props["Hovered"] = swapped;
                    swapped = new();
                    foreach (KeyValuePair<string, object> kvp in pressed) {
                        if (kvp.Value is Color c && hovered.TryGetValue(kvp.Key, out object? cNew)) {
                            swapped[kvp.Key] = cNew;
                        } else {
                            swapped[kvp.Key] = kvp.Value;
                        }
                    }
                    props["Pressed"] = swapped;
                }
            }

            Skin skin = CreateDump();

            foreach (Dictionary<string, object> props in skin.Map.Values)
                Convert(props);

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies()) {
                foreach (Type type in asm.GetTypes()) {
                    if (!typeof(Element).IsAssignableFrom(type) ||
                        type.GetField("DefaultStyleLight", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) is not Style style)
                        continue;

                    skin.Map[type.Name] = GenerateProps(style, skin.Map[type.Name]);
                }
            }

            return skin;
        }

        // Copied from RainbowMod.
        // Conversion algorithms found randomly on the net - best source for HSV <-> RGB ever:tm:

        private static void ColorToHSV(Color c, out float h, out float s, out float v) {
            float r = c.R / 255f;
            float g = c.G / 255f;
            float b = c.B / 255f;

            float min, max, delta;
            min = Math.Min(Math.Min(r, g), b);
            max = Math.Max(Math.Max(r, g), b);
            v = max;
            delta = max - min;

            if (max != 0) {
                s = delta / max;

                if (delta == 0)
                    h = 0;
                else if (r == max)
                    h = (g - b) / delta;
                else if (g == max)
                    h = 2 + (b - r) / delta;
                else
                    h = 4 + (r - g) / delta;

                h *= 60f;
                if (h < 0)
                    h += 360f;

            } else {
                s = 0f;
                h = 0f;
            }
        }

        private static Color ColorFromHSV(float hue, float saturation, float value) {
            int hi = (int) (Math.Floor(hue / 60f)) % 6;
            float f = hue / 60f - (float) Math.Floor(hue / 60f);

            value *= 255f;
            int v = (int) Math.Round(value);
            int p = (int) Math.Round(value * (1 - saturation));
            int q = (int) Math.Round(value * (1 - f * saturation));
            int t = (int) Math.Round(value * (1 - (1 - f) * saturation));

            return hi switch {
                0 => new Color(v, t, p, 255),
                1 => new Color(q, v, p, 255),
                2 => new Color(p, v, t, 255),
                3 => new Color(p, q, v, 255),
                4 => new Color(t, p, v, 255),
                _ => new Color(v, p, q, 255)
            };
        }

    }
}
