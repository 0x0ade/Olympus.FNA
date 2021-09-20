using FontStashSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OlympUI {
    public sealed class Style : IEnumerable<Style.Entry> {

        public static readonly Dictionary<Type, string> CommonNames = new() {
            { typeof(DynamicSpriteFont), "Font" },
        };

        private readonly Dictionary<string, object> Map = new();

        private readonly Dictionary<string, IFader> FaderMap = new();
        private readonly List<IFader> Faders = new();

        private readonly Dictionary<string, Link> LinkMap = new();
        private readonly List<Link> Links = new();

        private Style? Parent;

        public Type? Type;
        public Element? Element;

        private event Action? OnInvalidate;

        public Style() {
        }

        public Style(Element el) {
            Type = el.GetType();
            Element = el;

            SetupParent(Type);
        }

        public static string GetCommonName(Type type) {
            if (CommonNames.TryGetValue(type, out string? value))
                return value;

            if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Reloadable<>))
                return GetCommonName(type.GetGenericArguments()[0]);

            if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Reloadable<>))
                return GetCommonName(type.GetGenericArguments()[0]);

            return type.Name;
        }

        private void SetupParent(Type type) {
            if (Parent != null)
                return;

            if (Element != null) {
                Style style = this;
                for (Type? parentType = type; parentType != typeof(object) && parentType != null; parentType = parentType.BaseType) {
                    if (parentType.GetField("DefaultStyle", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) is Style parent) {
                        parent.SetupParent(parentType);
                        style.Parent = parent;
                        // The first parent that we meet should set up the rest of the parent chain.
                        break;
                    }
                }
                return;
            }

            if (Type == null) {
                Type = type;
                Style style = this;
                for (Type? parentType = type.BaseType; parentType != typeof(object) && parentType != null; parentType = parentType.BaseType) {
                    if (parentType.GetField("DefaultStyle", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) is Style parent) {
                        parent.SetupParent(parentType);
                        style.Parent = parent;
                        style = parent;
                    }
                }
            }
        }

        public void Add<T>(Func<T> value)
            => Add(GetCommonName(typeof(T)), (object) value);
        public void Add<T>(string key, Func<T> value)
            => Add(key, (object) value);
        public void Add(Link value)
            => Add(value.Key, (object) value);
        public void Add(string key, Link value)
            => Add(key, (object) value);
        public void Add<T>(Fader<T> value) where T : struct
            => Add(GetCommonName(typeof(T)), (object) value);
        public void Add<T>(string key, Fader<T> value) where T : struct
            => Add(key, (object) value);
        public void Add<T>(T value)
            => Add(GetCommonName(typeof(T)), value);
        public void Add(string key, object? value) {
            {
                if (LinkMap.TryGetValue(key, out Link? link)) {
                    link.Source.OnInvalidate -= Invalidate;
                    LinkMap.Remove(key);
                    Links.Remove(link);
                }
            }

            if (value == null) {
                Map.Remove(key);
                if (FaderMap.TryGetValue(key, out IFader? fader)) {
                    FaderMap.Remove(key);
                    Faders.Remove(fader);
                }
                return;
            }

            if (Map.TryGetValue(key, out object? raw)) {
                if (raw is IFader fader && fader.SetValueTo(value))
                    return;
            }

            if (Element != null) {
                for (Style? parent = Parent; parent != null; parent = parent.Parent) {
                    if (!parent.Map.TryGetValue(key, out raw))
                        continue;
                    if (raw is IFader fader) {
                        fader = fader.New();
                        if (fader.SetValueTo(value)) {
                            value = fader;
                            break;
                        }
                    }
                }
            }

            Map[key] = value;

            {
                if (value is IFader fader) {
                    FaderMap[key] = fader;
                    Faders.Add(fader);
                }

                if (value is Link link) {
                    LinkMap[key] = link;
                    Links.Add(link);
                    link.Source.OnInvalidate += Invalidate;
                }
            }
        }

        public void Clear()
            => Map.Clear();

        public bool TryGetCurrent<T>([NotNullWhen(true)] out T? value)
            => TryGetCurrent(GetCommonName(typeof(T)), out value);
        public bool TryGetCurrent<T>(string key, [NotNullWhen(true)] out T? value) {
            if (Map.TryGetValue(key, out object? raw)) {
                if (raw is Link link)
                    raw = link.Source.GetCurrent<T>(link.Key);
                if (raw is Func<T> cb)
                    raw = cb();
                if (raw is Reloadable<T> reloadable)
                    raw = reloadable.Value;
                if (raw is IFader fader) {
                    value = fader.GetValue<T>();
#pragma warning disable CS8762 // T cannot be nullable for faders.
                    return true;
#pragma warning restore CS8762
                }
                if (raw != default) {
                    value = (T) raw;
                    return true;
                }
            }

            if (Element != null) {
                for (Style? parent = Parent; parent != null; parent = parent.Parent)
                    if (parent.TryGetCurrent(key, out value))
                        return true;
            }

            value = default;
            return false;
        }

        public void GetCurrent<T>(out T value)
            => value = GetCurrent<T>(GetCommonName(typeof(T)));
        public void GetCurrent<T>(string key, out T value)
            => value = GetCurrent<T>(key);
        public T GetCurrent<T>()
            => GetCurrent<T>(GetCommonName(typeof(T)));
        public T GetCurrent<T>(string key)
            => TryGetCurrent(key, out T? value) ? value : throw new Exception($"{(Element == null ? "Instance style for" : "Static style for")} \"{Type}\" doesn't define \"{key}\"");

        public bool TryGetReal<T>([NotNullWhen(true)] out T? value)
            => TryGetReal(GetCommonName(typeof(T)), out value);
        public bool TryGetReal<T>(string key, [NotNullWhen(true)] out T? value) {
            if (Map.TryGetValue(key, out object? raw)) {
                if (raw is Link link)
                    raw = link.Source.GetReal<T>(link.Key);
                if (raw is Func<T> cb)
                    raw = cb();
                if (raw is Reloadable<T> reloadable)
                    raw = reloadable.Value;
                if (raw is IFader fader) {
                    value = fader.GetValueTo<T>();
#pragma warning disable CS8762 // T cannot be nullable for faders.
                    return true;
#pragma warning restore CS8762
                }
                if (raw != default) {
                    value = (T) raw;
                    return true;
                }
            }

            if (Element != null) {
                for (Style? parent = Parent; parent != null; parent = parent.Parent)
                    if (parent.TryGetReal(key, out value))
                        return true;
            }

            value = default;
            return false;
        }

        public void GetReal<T>(out T value)
            => value = GetReal<T>(GetCommonName(typeof(T)));
        public void GetReal<T>(string key, out T value)
            => value = GetReal<T>(key);
        public T GetReal<T>()
            => GetReal<T>(GetCommonName(typeof(T)));
        public T GetReal<T>(string key)
            => TryGetReal(key, out T? value) ? value : throw new Exception($"{(Element == null ? "Instance style for" : "Static style for")} \"{Type}\" doesn't define \"{key}\"");

        public void Apply(Style value) {
            if (value == null) {
                Clear();
            } else {
                foreach (Entry entry in value)
                    Add(entry.Key, entry.Value);
            }
        }

        public void Apply(string name) {
            {
                if (Map.TryGetValue(name, out object? raw) && raw is Style value)
                    Apply(value);
            }

            if (Element != null) {
                for (Style? parent = Parent; parent != null; parent = parent.Parent)
                    if (parent.Map.TryGetValue(name, out object? raw) && raw is Style value)
                        Apply(value);
            }
        }

        public Link GetLink(string key)
            => new(this, key);
        public Link GetLink<T>()
            => new(this, GetCommonName(typeof(T)));

        public void Update(float dt) {
            bool invalidate = false;

            foreach (IFader fader in Faders) {
                bool updated = fader.Update(dt);
                invalidate |= updated;
            }

            if (invalidate)
                Invalidate();
        }

        private void Invalidate() {
            OnInvalidate?.Invoke();
            Element?.InvalidatePaint();
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
        public IEnumerator<Entry> GetEnumerator() {
            HashSet<string> returned = new();
            
            foreach (KeyValuePair<string, object> kvp in Map)
                if (returned.Add(kvp.Key))
                    yield return new(this, kvp.Key, kvp.Value);

            if (Element != null) {
                for (Style? parent = Parent; parent != null; parent = parent.Parent)
                    foreach (Entry entry in parent)
                        if (returned.Add(entry.Key))
                            yield return entry;
            }
        }

        public class Entry {
            public readonly Style Owner;
            public readonly string Key;
            public readonly object Value;
            public Entry(Style owner, string key, object value) {
                Owner = owner;
                Key = key;
                Value = value;
            }
        }

        public class Link {
            public readonly Style Source;
            public readonly string Key;
            public Link(Style source, string key) {
                Source = source;
                Key = key;
            }
        }

    }
}
