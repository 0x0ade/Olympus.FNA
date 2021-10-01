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
            void Convert(Dictionary<string, object> props) {
                foreach (KeyValuePair<string, object> kvp in props.ToArray()) {
                    object value = kvp.Value;

                    {
                        if (value is Color c) {
                            props[kvp.Key] = value = c = new(255 - c.R, 255 - c.G, 255 - c.B, c.A);
                        }
                    }

                    if (value is FaderStub fader) {
                        if (fader.Value is Color c) {
                            fader.Value = c = new(255 - c.R, 255 - c.G, 255 - c.B, c.A);
                        }
                    }

                    if (value is Dictionary<string, object> dict) {
                        Convert(dict);
                    }
                }
            }

            Skin skin = CreateDump();
            foreach (Dictionary<string, object> props in skin.Map.Values)
                Convert(props);
            return skin;
        }

    }
}
