using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace OlympUI {
    public sealed class Data : IEnumerable<Data.Entry> {

        public static readonly Dictionary<Type, string> CommonNames = new() {
        };

        private readonly Dictionary<string, InnerEntry> Map = new();

        public Data() {
        }

        public static string GetCommonName(Type type) {
            if (CommonNames.TryGetValue(type, out string? value))
                return value;

            if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Reloadable<,>))
                return GetCommonName(type.GetGenericArguments()[0]);

            if (type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Reloadable<,>))
                return GetCommonName(type.GetGenericArguments()[0]);

            return type.Name;
        }

        public void Add<T>(T value)
            => Add(GetCommonName(typeof(T)), value);
        public void Add<T>(string key, T? value) {
            if (value is null)
                Map.Remove(key);
            else
                Map[key] = new InnerEntry<T>(value);
        }

        public void Clear()
            => Map.Clear();

        public bool TryGet<T>([NotNullWhen(true)] out T? value)
            => TryGet(GetCommonName(typeof(T)), out value);
        public bool TryGet<T>(string key, out T? value) {
            if (Map.TryGetValue(key, out InnerEntry? rawRaw) && rawRaw is InnerEntry<T> raw) {
                value = raw.Value;
                return true;
            }

            value = default;
            return false;
        }

        public T? Get<T>()
            => Get<T>(GetCommonName(typeof(T)));
        public T? Get<T>(string key)
            => TryGet(key, out T? value) ? value : throw new Exception($"Data not found: \"{key}\"");

        public ref T? Ref<T>()
            => ref Ref<T>(GetCommonName(typeof(T)));
        public ref T? Ref<T>(string key) {
            if (Map.TryGetValue(key, out InnerEntry? rawRaw) && rawRaw is InnerEntry<T> raw)
                return ref raw.Value;
            throw new Exception($"Data not found: \"{key}\"");
        }

        public void Apply(Data value) {
            if (value is null) {
                Clear();
            } else {
                foreach (Entry entry in value)
                    Add(entry.Key, entry.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();
        public IEnumerator<Entry> GetEnumerator() {
            HashSet<string> returned = new();

            foreach (KeyValuePair<string, InnerEntry> kvp in Map)
                if (returned.Add(kvp.Key))
                    yield return new(this, kvp.Key, kvp.Value.ObjectValue);
        }

        public class Entry {
            public readonly Data Owner;
            public readonly string Key;
            public readonly object? Value;
            public Entry(Data owner, string key, object? value) {
                Owner = owner;
                Key = key;
                Value = value;
            }
        }

        private abstract class InnerEntry {
            public abstract object? ObjectValue { get; set; }
        }

        private class InnerEntry<T> : InnerEntry {
            public T? Value;
            public override object? ObjectValue {
                get => Value;
                set => Value = (T?) value;
            }
            public InnerEntry(T value) {
                Value = value;
            }
        }

    }
}
