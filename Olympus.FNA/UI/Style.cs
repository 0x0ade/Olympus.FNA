using FontStashSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OlympUI {
    public sealed class Style : IEnumerable<Style.Entry> {

        public static readonly Dictionary<Type, string> CommonNames = new() {
            { typeof(DynamicSpriteFont), "Font" },
        };

        private static readonly Dictionary<string, string> ExpressionNames = new();

        // This must sadly be an ordered list for update order. A dict can be added alongside this later if needed.
        public static readonly List<Style> TypeStyles = new();
        public static readonly Dictionary<(Type From, Type To), IConverter> Converters = new();

        private readonly Dictionary<string, Entry> Map = new();

        private Style? Parent;

        public Type? Type;
        public Element? Element;

        private event Action? OnInvalidate;
        private uint InvalidateID;
        private bool IsHierarchyInvalidated {
            get {
                for (Style? style = Parent; style is not null; style = style.Parent)
                    if (style.InvalidateID == UI.GlobalDrawID)
                        return true;
                return false;
            }
        }

        static Style() {
            foreach (Type type in UIReflection.GetAllTypes(typeof(IConverter))) {
                if (type.IsAbstract)
                    continue;

                IConverter conv = (IConverter) Activator.CreateInstance(type)!;
                foreach ((Type, Type) mapping in conv.Supported)
                    Converters[mapping] = conv;
            }
        }

        public Style() {
        }

        public Style(Element el) {
            Type = el.GetType();
            Element = el;

            SetupParent(Type);
        }

        public static T Convert<T>(object raw) {
            if (Converters.TryGetValue((raw.GetType(), typeof(T)), out IConverter? conv))
                return conv.Convert<T>(raw);
            return (T) System.Convert.ChangeType(raw, typeof(T));
        }

        public static string GetCommonName(Type type) {
            if (CommonNames.TryGetValue(type, out string? value))
                return value;

            for (Type? typeBase = type; typeBase is not null; typeBase = typeBase.BaseType) {
                if (typeBase.IsConstructedGenericType && typeBase.GetGenericTypeDefinition() == typeof(Reloadable<,>))
                    return GetCommonName(typeBase.GetGenericArguments()[0]);

                if (typeBase.IsConstructedGenericType && typeBase.GetGenericTypeDefinition() == typeof(IReloadable<,>))
                    return GetCommonName(typeBase.GetGenericArguments()[0]);

                if (typeBase.IsConstructedGenericType && typeBase.GetGenericTypeDefinition() == typeof(Fader<>))
                    return GetCommonName(typeBase.GetGenericArguments()[0]);
            }

            return type.Name;
        }

        public static string? GetKeyFromCallerArgumentExpression(string? expr) {
            if (string.IsNullOrEmpty(expr))
                return null;

            if (ExpressionNames.TryGetValue(expr, out string? value))
                return value;

            int split = expr.IndexOf(' ');
            string sub = char.ToUpperInvariant(expr[split + 1]) + expr.Substring(split + 2);
            if (sub.StartsWith("Style"))
                sub = expr.Substring(5);
            return ExpressionNames[expr] = sub;
        }

        private void SetupParent(Type type) {
            if (Parent is not null)
                return;

            if (Element is not null) {
                Style style = this;
                for (Type? parentType = type; parentType != typeof(object) && parentType is not null; parentType = parentType.BaseType) {
                    if (parentType.GetField("DefaultStyle", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) is Style parent) {
                        parent.SetupParent(parentType);
                        style.Parent = parent;
                        // The first parent that we meet should set up the rest of the parent chain.
                        break;
                    }
                }

            } else if (Type is null) {
                Type = type;
                TypeStyles.Insert(0, this);
                Style style = this;
                for (Type? parentType = type.BaseType; parentType != typeof(object) && parentType is not null; parentType = parentType.BaseType) {
                    if (parentType.GetField("DefaultStyle", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) is Style parent) {
                        parent.SetupParent(parentType);
                        style.Parent = parent;
                        style = parent;
                    }
                }
            }

            if (Parent is not null)
                foreach (Entry entry in Map.Values)
                    entry.Parent = Parent.GetEntry(entry.Key);
        }

        public void GetEntry<T>(out Entry value)
            => value = GetEntry(GetCommonName(typeof(T)));
#if !OLYMPUS_STYLE_NOEXPR
        public void GetEntry(out Entry value, [CallerArgumentExpression("value")] string? expr = null)
            => value = GetEntry(GetKeyFromCallerArgumentExpression(expr) ?? throw new ArgumentException($"Couldn't parse entry name from \"{expr}\"", nameof(value)));
#endif
        public Entry GetEntry<T>()
            => GetEntry(GetCommonName(typeof(T)));
        public Entry GetEntry(string key) {
            if (Map.TryGetValue(key, out Entry? entry))
                return entry;
            return Map[key] = new(this, key, Parent?.GetEntry(key));
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
        public void Add(string key, object? value)
            => GetEntry(key).Value = value;

        public void Clear()
            => Map.Clear();

#if OLYMPUS_STYLE_NOEXPR
        public bool TryGetCurrent<T>([NotNullWhen(true)] out T? value)
            => TryGetCurrent(GetCommonName(typeof(T)), out value);
#else
        public bool TryGetCurrent<T>([NotNullWhen(true)] out T? value, [CallerArgumentExpression("value")] string? expr = null)
            => TryGetCurrent(GetKeyFromCallerArgumentExpression(expr) ?? GetCommonName(typeof(T)), out value);
#endif
        public bool TryGetCurrent<T>(string key, [NotNullWhen(true)] out T? value)
            => GetEntry(key).TryGetCurrent(out value);

#if OLYMPUS_STYLE_NOEXPR
        public void GetCurrent<T>(out T value)
            => value = GetCurrent<T>(GetCommonName(typeof(T)));
#else
        public void GetCurrent<T>(out T value, [CallerArgumentExpression("value")] string? expr = null)
            => value = GetCurrent<T>(GetKeyFromCallerArgumentExpression(expr) ?? GetCommonName(typeof(T)));
#endif
        public void GetCurrent<T>(string key, out T value)
            => value = GetCurrent<T>(key);
        public T GetCurrent<T>()
            => GetCurrent<T>(GetCommonName(typeof(T)));
        public T GetCurrent<T>(string key)
            => TryGetCurrent(key, out T? value) ? value : throw new Exception($"{(Element is null ? "Instance style for" : "Static style for")} \"{Type}\" doesn't define \"{key}\"");

#if OLYMPUS_STYLE_NOEXPR
        public bool TryGetReal<T>([NotNullWhen(true)] out T? value)
            => TryGetReal(GetCommonName(typeof(T)), out value);
#else
        public bool TryGetReal<T>([NotNullWhen(true)] out T? value, [CallerArgumentExpression("value")] string? expr = null)
            => TryGetReal(GetKeyFromCallerArgumentExpression(expr) ?? GetCommonName(typeof(T)), out value);
#endif
        public bool TryGetReal<T>(string key, [NotNullWhen(true)] out T? value)
            => GetEntry(key).TryGetReal(out value);

#if OLYMPUS_STYLE_NOEXPR
        public void GetReal<T>(out T value)
            => value = GetReal<T>(GetCommonName(typeof(T)));
#else
        public void GetReal<T>(out T value, [CallerArgumentExpression("value")] string? expr = null)
            => value = GetReal<T>(GetKeyFromCallerArgumentExpression(expr) ?? GetCommonName(typeof(T)));
#endif
        public void GetReal<T>(string key, out T value)
            => value = GetReal<T>(key);
        public T GetReal<T>()
            => GetReal<T>(GetCommonName(typeof(T)));
        public T GetReal<T>(string key)
            => TryGetReal(key, out T? value) ? value : throw new Exception($"{(Element is null ? "Instance style for" : "Static style for")} \"{Type}\" doesn't define \"{key}\"");

        public void Apply(Style value) {
            if (value is null) {
                foreach (KeyValuePair<string, Entry> entry in Map)
                    GetEntry(entry.Key).Value = null;
            } else {
                foreach (Entry entry in value)
                    Add(entry.Key, entry.Value);
            }
        }

        public void Apply(string name) {
            if (Element is not null) {
                // FIXME: Cache style parent stack!
                Stack<Style> stack = new();
                for (Style? parent = Parent; parent is not null; parent = parent.Parent)
                    stack.Push(parent);
                foreach (Style parent in stack) {
                    Entry entry = parent.GetEntry(name);
                    if (entry.Value is Style value)
                        Apply(value);
                    if (entry.GetSkinnedRaw() is Dictionary<string, object> props)
                        foreach (KeyValuePair<string, object> prop in props)
                            Add(prop.Key, prop.Value);
                }
            }

            {
                Entry entry = GetEntry(name);
                if (entry.Value is Style value)
                    Apply(value);
                if (entry.GetSkinnedRaw() is Dictionary<string, object> props)
                    foreach (KeyValuePair<string, object> prop in props)
                        Add(prop.Key, prop.Value);
            }
        }

        public Link GetLink(string key)
            => new(this, key);
        public Link GetLink<T>()
            => new(this, GetCommonName(typeof(T)));

        public void Revive() {
            foreach (Entry entry in Map.Values)
                if (entry.Value is IFader fader)
                    fader.Revive();
        }

        public static void UpdateTypeStyles(float dt) {
            foreach (Style style in TypeStyles) {
                style.Update(dt);
            }
        }

        public void Update(float dt) {
            bool invalidate = IsHierarchyInvalidated;

            foreach (Entry entry in Map.Values)
                if (entry.Value is IFader fader)
                    invalidate |= fader.Update(dt);

            if (invalidate)
                Invalidate();
        }

        private void Invalidate() {
            InvalidateID = UI.GlobalUpdateID;
            OnInvalidate?.Invoke();
            Element?.InvalidatePaint();
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
        public IEnumerator<Entry> GetEnumerator() {
            HashSet<string> returned = new();

            foreach (KeyValuePair<string, Entry> kvp in Map)
                if (!kvp.Value.IsUnset && returned.Add(kvp.Key))
                    yield return kvp.Value;

            if (Element is not null) {
                for (Style? parent = Parent; parent is not null; parent = parent.Parent)
                    foreach (KeyValuePair<string, Entry> kvp in parent.Map)
                        if (!kvp.Value.IsUnset && returned.Add(kvp.Key))
                            yield return kvp.Value;
            }
        }

        public class Entry {

            public readonly Style Style;
            public readonly string Key;
            public Entry? Parent;

            private object? _Value;
            private Link? _ValueAsLink;
            private IReloadable? _ValueAsReloadable;
            private IFader? _ValueAsFader;
            private ValueType _ValueType;

            private Skin.FaderStub? _SkinnedFader;
            private Skin.FaderStub? _SkinnedFaderPending;

            public object? Value {
                get => _Value;
                set {
                    if (value is null) {
                        ForceSetValue(null);
                        return;
                    }

                    if (_ValueAsFader is IFader faderOld) {
                        if (value is IFader faderOther) {
                            faderOld.Deserialize(faderOther.GetSerializedDuration(), faderOther.GetSerializedValue());
                        } else {
                            faderOld.SetValueTo(value);
                        }
                        // If we don't remove skinned faders here, they'll override our override again.
                        _SkinnedFader = null;
                        _SkinnedFaderPending = null;
                        return;
                    }

                    if (_SkinnedFader is Skin.FaderStub faderStub) {
                        _SkinnedFader = null;
                        _SkinnedFaderPending = null;
                        value = Fader.Create(value.GetType(), faderStub.Fade, value);
                    }

                    value = ForceSetValue(value);

                    if (value is IFader) {
                        _SkinnedFader = null;
                        _SkinnedFaderPending = null;
                    }
                }
            }

            private object? ForceSetValue(object? value) {
                if (_ValueAsLink is not null)
                    _ValueAsLink.Source.OnInvalidate -= Style.Invalidate;

                _ValueAsLink = null;
                _ValueAsReloadable = null;
                _ValueAsFader = null;
                IsUnset = false;
                IsInherited = false;

                if (value is null) {
                    _Value = value;
                    _ValueType = ValueType.Inherit;
                    IsInherited = true;
                    return value;
                }

                if (Style.Element is not null) {
                    for (Style? parent = Style.Parent; parent is not null; parent = parent.Parent) {
                        if (!parent.Map.TryGetValue(Key, out Entry? parentEntry))
                            continue;
                        if (parentEntry.Value is IFader fader) {
                            fader = fader.Clone();
                            if (fader.SetValueTo(value)) {
                                value = fader;
                                break;
                            }
                        }
                    }
                }

                _Value = value;

                if (value is Link link) {
                    _ValueType = ValueType.Link;
                    _ValueAsLink = link;
                    link.Source.OnInvalidate += Style.Invalidate;

                } else if (value is Delegate) {
                    _ValueType = ValueType.Func;

                } else if (value is IReloadable reloadable) {
                    _ValueType = ValueType.Reloadable;
                    _ValueAsReloadable = reloadable;

                } else if (value is IFader fader) {
                    _ValueType = ValueType.Fader;
                    _ValueAsFader = fader;

                } else {
                    _ValueType = ValueType.Raw;
                }

                return value;
            }

            public bool IsUnset = true;
            public bool IsInherited;

            public readonly bool IsDummy;

            public Entry(Style owner, string key, Entry? parent) {
                IsDummy = false;
                Style = owner;
                Key = key;
                Parent = parent;
                Value = null;
            }

            public Entry(object value) {
                IsDummy = true;
            }

            public void CheckDummy() {
                if (IsDummy)
                    throw new InvalidOperationException("Dummy style entry!");
            }

            public void GetCurrent<T>(out T value)
                => value = GetCurrent<T>();
            public T GetCurrent<T>()
                => TryGetCurrent(out T? value) ? value : throw new Exception($"{(Style.Element is null ? "Instance style for" : "Static style for")} \"{Style.Type}\" has got null \"{Key}\"");

            public void GetReal<T>(out T value)
                => value = GetReal<T>();
            public T GetReal<T>()
                => TryGetReal(out T? value) ? value : throw new Exception($"{(Style.Element is null ? "Instance style for" : "Static style for")} \"{Style.Type}\" has got null \"{Key}\"");

            public object? GetSkinnedRaw() {
                CheckDummy();

                object? raw = default;

                if (Style.Type is Type type && Skin.Current is Skin skin) {
                    if (skin.TryGetValue(type.Name, Key, out raw) && raw != default)
                        return raw;

                    // Links are pain. Someone please save me from this hell.
                    {
                        if (_ValueAsLink?.Source.GetEntry(Key).GetSkinnedRaw() is object got)
                            return got;
                    }

                    if (Style.Element is not null) {
                        foreach (string @class in Style.Element.Classes)
                            if (skin.TryGetValue(@class, Key, out raw) && raw != default)
                                return raw;
                    }

                    {
                        if (Parent?.GetSkinnedRaw() is object got)
                            return got;
                    }
                }

                return raw;
            }

            private bool TryGetSkinnedAndPrepare<T>([NotNullWhen(true)] out T? value) {
                object? skinnedRaw = GetSkinnedRaw();
                if (skinnedRaw is Skin.FaderStub faderStub) {
                    if (Value is not IFader fader) {
                        _SkinnedFader = faderStub;
                        _SkinnedFaderPending = faderStub;
                        if (faderStub.Value is not null) {
                            ForceSetValue(Fader.Create(faderStub.Type ?? throw new Exception("Typeless fader stub"), faderStub.Fade, faderStub.Value));
                        } else if (TryGetReal(out T? valueReal)) {
                            ForceSetValue(Fader.Create(typeof(T), faderStub.Fade, valueReal));
                        }

                    } else if (_SkinnedFader is Skin.FaderStub faderStubOld ? faderStub != faderStubOld : IsUnset) {
                        _SkinnedFader = faderStub;
                        _SkinnedFaderPending = faderStub;
                        fader.Deserialize(faderStub.Fade, faderStub.Value);
                    }
                } else if (_SkinnedFader is not null && Value is IFader) {
                    _SkinnedFader = null;
                    _SkinnedFaderPending = null;
                    ForceSetValue(null);
                }

                if (skinnedRaw is T skinned) {
                    value = skinned;
                    return true;
                }

                value = default;
                return false;
            }

            public bool TryGetCurrent<T>([NotNullWhen(true)] out T? value) {
                CheckDummy();

                bool skinnedSet = TryGetSkinnedAndPrepare(out T? skinned);

                if (Style.Element is null && skinnedSet) {
                    value = skinned;
#pragma warning disable CS8762 // skinnedSet being true is enough proof of skinned being non-null.
                    return true;
#pragma warning restore CS8762
                }

                bool valueSet;
                switch (_ValueType) {
                    case ValueType.Raw:
                    default:
                        value = Convert<T>(Value!);
                        valueSet = true;
                        break;

                    case ValueType.Inherit:
                        value = default;
                        valueSet = false;
                        break;

                    case ValueType.Link:
                        valueSet = _ValueAsLink!.Source.TryGetCurrent(_ValueAsLink.Key, out value);
                        break;

                    case ValueType.Func:
                        value = (Value as Func<T>)!();
                        valueSet = value is not null;
                        break;

                    case ValueType.Reloadable:
                        value = _ValueAsReloadable!.GetValue<T>();
                        valueSet = value is not null;
                        break;

                    case ValueType.Fader:
                        value = _ValueAsFader!.GetValue<T>();
#pragma warning disable CS8762 // T cannot be nullable for faders.
                        // Skip SkinnedFadersPending check.
                        return true;
#pragma warning restore CS8762
                }

                if (valueSet && _SkinnedFaderPending is Skin.FaderStub faderStub) {
                    _SkinnedFaderPending = null;
#pragma warning disable CS8602 // valueSet being true is enough proof of value being non-null.
                    IFader fader = Fader.Create(value.GetType(), faderStub.Fade, value);
#pragma warning restore CS8602
                    Value = fader;
                }

#pragma warning disable CS8762 // valueSet being true is enough proof of value being non-null.
                if (valueSet)
                    return true;
#pragma warning restore CS8762

                if (skinnedSet) {
                    value = skinned;
#pragma warning disable CS8762 // skinnedSet being true is enough proof of skinned being non-null.
                    return true;
#pragma warning restore CS8762
                }

                if (Parent is not null)
                    return Parent.TryGetCurrent(out value);

                return false;
            }

            public bool TryGetReal<T>([NotNullWhen(true)] out T? value) {
                CheckDummy();

                switch (_ValueType) {
                    case ValueType.Raw:
                    default:
                        value = Convert<T>(Value!);
#pragma warning disable CS8762 // Convert<T> returns non-null.
                        return true;
#pragma warning restore CS8762

                    case ValueType.Inherit:
                        value = default;
                        break;

                    case ValueType.Link:
                        if (_ValueAsLink!.Source.TryGetReal(_ValueAsLink.Key, out value))
                            return true;
                        break;

                    case ValueType.Func:
                        value = (Value as Func<T>)!();
                        if (value is not null)
                            return true;
                        break;

                    case ValueType.Reloadable:
                        value = _ValueAsReloadable!.GetValue<T>();
                        if (value is not null)
                            return true;
                        break;

                    case ValueType.Fader:
                        value = _ValueAsFader!.GetValueTo<T>();
#pragma warning disable CS8762 // T cannot be nullable for faders.
                        return true;
#pragma warning restore CS8762
                }

                if (Parent is not null)
                    return Parent.TryGetReal(out value);

                return false;
            }

            private enum ValueType {
                Raw,
                Inherit,
                Link,
                Func,
                Reloadable,
                Fader,
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

        public interface IConverter {
            (Type From, Type To)[] Supported { get; }
            T Convert<T>(object raw);
        }

    }
}
