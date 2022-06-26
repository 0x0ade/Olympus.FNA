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

        private static readonly IDeserializer Deserializer = new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithTypeConverter(new ColorYamlTypeConverter())
            .Build();

        private static readonly ISerializer Serializer = new SerializerBuilder()
            .ConfigureDefaultValuesHandling(DefaultValuesHandling.Preserve)
            .WithTypeConverter(new ColorYamlTypeConverter())
            .WithTypeConverter(new SkinYamlTypeConverter())
            .WithEventEmitter(next => new SkinYamlEventEmitter(next))
            .Build();

        public static Skin? Current;

        static Skin() {
            // beforefieldinit
        }

        public static void Serialize(TextWriter writer, Skin skin) {
            Serializer.Serialize(writer, skin);
        }

        public static Skin Deserialize(TextReader reader) {
            Skin skin = Deserializer.Deserialize<Skin>(reader);
            foreach (Dictionary<string, object> props in skin.Map.Values)
                Fixup(props);
            return skin;
        }

        private static void Fixup(Dictionary<string, object> props) {
            foreach (KeyValuePair<string, object> kvp in props.ToArray()) {
                object value = kvp.Value;
                Dictionary<string, object>? dict;

                if (value is string str) {
                    if (int.TryParse(str, out int i)) {
                        props[kvp.Key] = value = i;
                    } else if (float.TryParse(str, out float f)) {
                        props[kvp.Key] = value = f;
                    } else if (UIMath.TryParseHexColor(str, out Color c)) {
                        props[kvp.Key] = value = c;
                    }
                }

                if (value is Dictionary<object, object> dictRaw) {
                    dict = new();
                    foreach (KeyValuePair<object, object> kvpNest in dictRaw)
                        dict[kvpNest.Key?.ToString() ?? ""] = kvpNest.Value;
                    props[kvp.Key] = value = dict;
                }

                if ((dict = value as Dictionary<string, object>) is not null) {
                    Fixup(dict);
                    if (dict.TryGetValue("Fade", out object? fadeRaw) && fadeRaw is float fade) {
                        FaderStub fader = new() {
                            Fade = fade,
                        };
                        if (dict.TryGetValue("Value", out object? fadeValue) && fadeValue is not null &&
                            !(fadeValue is string fadeText && string.IsNullOrEmpty(fadeText))) {
                            fader.Value = fadeValue;
                        }
                        props[kvp.Key] = value = fader;
                    }
                }
            }
        }

        public static Skin CreateDump() {
            Skin skin = new();

            foreach (Type type in UIReflection.GetAllTypes(typeof(Element))) {
                if (type.GetField("DefaultStyle", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) is not Style style)
                    continue;

                skin.Map[type.Name] = GenerateProps(style, new());
            }

            return skin;
        }

        private static Dictionary<string, object> GenerateProps(Style style, Dictionary<string, object> props) {
            foreach (Style.Entry entry in style) {
                string key = entry.Key.ToString();
                object raw = entry.Value;
                if (raw is Style substyle) {
                    if (!props.TryGetValue(key, out object? subpropsRaw) || subpropsRaw is not Dictionary<string, object> subprops)
                        subprops = new();
                    raw = GenerateProps(substyle, subprops);

                } else if (raw is IFader fader) {
                    raw = new FaderStub() {
                        Type = fader.GetSerializedType(),
                        Fade = fader.GetSerializedDuration(),
                        Value = fader.GetSerializedValue()
                    };

                } else if (
                    raw is Style.Link ||
                    raw is MulticastDelegate ||
                    raw is IDisposable
                ) {
                    continue;
                }

                props[key] = raw;
            }
            return props;
        }

        public Dictionary<string, Dictionary<string, object>> Map { get; set; } = new();

        public bool TryGetValue(string type, string key, [NotNullWhen(true)] out object? value) {
            value = null;
            return
                Map.TryGetValue(type, out Dictionary<string, object>? props) &&
                props.TryGetValue(key, out value);
        }

        public T Get<T>(params string[] keys) {
            Dictionary<string, object> props = Map[keys[0]];
            for (int i = 1; i < keys.Length - 1; i++) {
                props = (Dictionary<string, object>) props[keys[i]];
            }
            return (T) props[keys[^1]];
        }

        public void Set(object value, params string[] keys) {
            Dictionary<string, object> props = Map[keys[0]];
            for (int i = 1; i < keys.Length - 1; i++) {
                props = (Dictionary<string, object>) props[keys[i]];
            }
            props[keys[^1]] = value;
        }

        public class FaderStub {

            public Type? Type { get; set; }
            public float Fade { get; set; } = 0.15f;
            public object? Value { get; set; }

            public override bool Equals(object? obj) {
                if (obj is not FaderStub other)
                    return false;
                return Type == other.Type && Fade == other.Fade && Value == other.Value;
            }

            public override int GetHashCode() {
                return HashCode.Combine(Type, Fade, Value);
            }

        }

        public class SkinYamlTypeConverter : IYamlTypeConverter {

            public bool Accepts(Type type)
                => type == typeof(float);

            public object ReadYaml(IParser parser, Type type) {
                if (type == typeof(float)) {
                    return float.Parse(parser.Consume<Scalar>().Value);
                }

                throw new Exception($"Unexpected type: {type}");
            }

            public void WriteYaml(IEmitter emitter, object? value, Type type) {
                if (value is float f) {
                    string str = f.ToString();
                    if (!str.Contains('.'))
                        str = f.ToString("F1");
                    emitter.Emit(new Scalar(str));
                    return;
                }

                throw new Exception($"Unexpected type: {type} ({value?.GetType()})");
            }

        }

        private class SkinYamlEventEmitter : ChainedEventEmitter {

            public SkinYamlEventEmitter(IEventEmitter nextEmitter)
                : base(nextEmitter) {
            }

            public override void Emit(MappingStartEventInfo eventInfo, IEmitter emitter) {
                if (typeof(FaderStub).IsAssignableFrom(eventInfo.Source.Type)) {
                    eventInfo.Style = MappingStyle.Flow;
                }

                nextEmitter.Emit(eventInfo, emitter);
            }

        }

    }
}
