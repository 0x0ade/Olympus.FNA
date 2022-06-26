using Microsoft.Xna.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace OlympUI {
    public sealed class Faders : IEnumerable<(Style.Key, IFader)> {

        private List<Action<Faders>> Links = new();
        private List<(Style.Key, IFader)> List = new();
        private Dictionary<Style.Key, IFader> Map = new();

        public void Link(Action<Faders> cb)
            => Links.Add(cb);

        public void LinkInvalidatePaint(Element element)
            => Links.Add(_ => element.InvalidatePaint());

        public void LinkSetStyle(Element element, Style.Key key)
            => Links.Add(faders => {
                foreach ((Style.Key Key, IFader Fader) entry in faders.List)
                    entry.Fader.SetStyle(element.Style, entry.Key);
                element.InvalidatePaint();
            });

        private void UpdateLinks() {
            foreach (Action<Faders> link in Links)
                link(this);
        }

        public void Add(Style.Key key, IFader fader) {
            List.Add((key, fader));
            Map.Add(key, fader);
        }

        public bool Update(float dt, Style style) {
            bool updated = false;
            foreach ((Style.Key Key, IFader Fader) entry in List) {
                bool subupdated = entry.Fader.Update(dt, style, entry.Key);
                updated |= subupdated;
            }
            if (updated)
                UpdateLinks();
            return updated;
        }

        public T Get<T>(Style.Key key) where T : struct
            => ((Fader<T>) Map[key]).Value;

        public void Get<T>(Style.Key key, out T value) where T : struct
            => value = ((Fader<T>) Map[key]).Value;

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        public IEnumerator<(Style.Key, IFader)> GetEnumerator()
            => List.GetEnumerator();

    }

    public interface IFader : IGenericValueSource {

        bool IsFresh { get; }

        Type GetSerializedType();

        float GetSerializedDuration();

        object? GetSerializedValue();

        void Deserialize(float duration, object? value);

        T GetValueTo<T>();

        bool SetValueTo(object value);

        void Link(Action<IFader> cb);

        void LinkInvalidatePaint(Element element);

        void LinkSetStyle(Element element, Style.Key key);

        void SetStyle(Style style, Style.Key key);

        void Revive();

        bool Update(float dt);

        bool Update(float dt, Style style);

        bool Update(float dt, Style style, Style.Key key);

        IFader Clone();

    }

    public static class Fader {

        private static Dictionary<Type, Type> TypeCache = new();

        public static IFader Create(Type valueType, float duration, object? value) {
            if (!TypeCache.TryGetValue(valueType, out Type? faderType)) {
                Type faderBaseType = typeof(Fader<>).MakeGenericType(valueType);
                faderType = UIReflection.GetAllTypes(faderBaseType).FirstOrDefault();
                if (faderType is null)
                    throw new Exception($"Couldn't find fader type compatible with {valueType}");
                TypeCache[valueType] = faderType;
            }

            IFader fader = Activator.CreateInstance(faderType) as IFader ?? throw new Exception($"Couldn't create instance of type {faderType}");
            fader.Deserialize(duration, value);
            return fader;
        }

    }

    public abstract class Fader<T> : IFader where T : struct {

        private List<Action<Fader<T>>> Links = new();

        public T Value;
        public T ValueFrom;

        private T _ValueTo;
        private bool _ValueToSet;
        private Func<T>? _ValueToSource;
        public T ValueTo {
            get => _ValueToSource?.Invoke() ?? _ValueTo;
            set => _ = (_ValueTo = value, _ValueToSet = true, _ValueToSource = null);
        }

        public float Time = -1f;
        public float Duration = 0.2f;

        private T ValueFromPrev;
        private T ValueToPrev;
        private float TPrev;

        public bool IsFresh => Time < 0f;

        protected Fader(bool valueSet, T value) {
            Value = ValueFrom = _ValueTo = value;
            _ValueToSet = valueSet;
        }

        protected Fader(Func<T> value) {
            _ValueToSource = value;
            _ValueToSet = true;
            Value = ValueFrom = ValueTo;
        }

        public abstract T Calculate(T a, T b, float t);
        protected abstract bool Equal(T a, T b);
        protected abstract Fader<T> New();

        public Fader<T> Clone() {
            Fader<T> f = New();
            if (!_ValueToSet)
                return f;

            f._ValueToSet = true;

            if (_ValueToSource is not null) {
                f._ValueToSource = _ValueToSource;
            } else {
                f._ValueTo = _ValueTo;
            }

            f.Value = f.ValueFrom = f.ValueTo;
            return f;
        }

        Type IFader.GetSerializedType()
            => typeof(T);

        float IFader.GetSerializedDuration()
            => Duration;

        object? IFader.GetSerializedValue()
            => _ValueToSet ? _ValueToSource ?? (object) _ValueTo : null;

        void IFader.Deserialize(float duration, object? value) {
            Duration = duration;
            if (value is null) {
                if (_ValueToSet) {
                    _ValueTo = default;
                } else {
                    Value = ValueFrom = _ValueTo = default;
                    _ValueToSet = false;
                }
            } else if (value is T valueReal) {
                if (_ValueToSet) {
                    _ValueTo = valueReal;
                } else {
                    Value = ValueFrom = _ValueTo = valueReal;
                    _ValueToSet = true;
                }
            } else if (value is Func<T> valueSource) {
                if (_ValueToSet) {
                    _ValueToSource = valueSource;
                } else {
                    _ValueToSource = valueSource;
                    _ValueToSet = true;
                    Value = ValueFrom = ValueTo;
                }
            } else {
                throw new Exception($"Unexpected value of type {value.GetType()} instead of {typeof(T)}");
            }
        }

        TGet IGenericValueSource.GetValue<TGet>() {
            if (typeof(T) == typeof(TGet))
                return Unsafe.As<T, TGet>(ref Value);
            return (TGet) Convert.ChangeType(Value, typeof(TGet));
        }

        TGet IFader.GetValueTo<TGet>() {
            T value = ValueTo;
            if (typeof(T) == typeof(TGet))
                return Unsafe.As<T, TGet>(ref value);
            return (TGet) Convert.ChangeType(value, typeof(TGet));
        }

        bool IFader.SetValueTo(object value) {
            if (value is T raw) {
                _ValueToSet = true;
                _ValueTo = raw;
                _ValueToSource = null;
                return true;
            }
            if (value is Func<T> rawSource) {
                _ValueToSet = true;
                _ValueToSource = rawSource;
                return true;
            }
            return false;
        }

        public void Link(Action<Fader<T>> cb)
            => Links.Add(cb);

        public void Link(Action<IFader> cb)
            => Links.Add(fader => cb(fader));

        public void LinkInvalidatePaint(Element element)
            => Links.Add(_ => element.InvalidatePaint());

        public void LinkSetStyle(Element element, Style.Key key)
            => Links.Add(fader => {
                element.Style.Add(key, fader.Value);
                element.InvalidatePaint();
            });

        private void UpdateLinks() {
            foreach (Action<Fader<T>> link in Links)
                link(this);
        }

        public void SetStyle(Style style, Style.Key key) {
            style.Add(key, Value);
        }

        public void Revive() {
            // FIXME: -1f should be enough for fader revives!
            Time = -2f;
            UpdateLinks();
        }

        public bool Update(float dt) {
            T valueTo = ValueTo;

            if (Time < 0f) {
                // FIXME: After a revive, this can still hold the old value!
                Value = ValueFrom = valueTo;
                Time += 1f;
                if (Time >= 0f) {
                    Time = Duration;
                }
                UpdateLinks();
                return true;
            }

            bool force = !Equal(ValueToPrev, valueTo);
            if (force) {
                if (Equal(ValueFromPrev, valueTo) && Equal(ValueToPrev, ValueFrom)) {
                    (ValueFromPrev, ValueToPrev) = (ValueToPrev, ValueFromPrev);
                    ValueFrom = Value;
                    Time = Duration - TPrev * Duration;
                } else {
                    ValueFromPrev = ValueFrom = Value;
                    ValueToPrev = valueTo;
                    Time = dt;
                }

            } else {
                if (Time >= Duration) {
                    return false;
                }

                Time += dt;
            }

            if (Time >= Duration) {
                Time = Duration;
                force = true;
            }

            if (!force && Time >= Duration)
                return false;

            float t = 1f - Time / Duration;
            TPrev = t = 1f - t * t;
            T next = Calculate(ValueFrom, valueTo, t);
            bool changed = !Equal(Value, next);
            Value = next;
            UpdateLinks();
            return changed;
        }

        public bool Update(float dt, Style style) {
            ValueTo = style.GetReal<T>();
            return Update(dt);
        }

        public bool Update(float dt, Style style, Style.Key key) {
            ValueTo = style.GetReal<T>(key);
            return Update(dt);
        }

        IFader IFader.Clone()
            => Clone();

    }

    public sealed class FloatFader : Fader<float> {

        public FloatFader()
            : base(false, default) {
        }

        public FloatFader(float value)
            : base(true, value) {
        }

        public FloatFader(Func<float> value)
            : base(value) {
        }

        public override float Calculate(float a, float b, float t)
            => a + (b - a) * t;

        protected override bool Equal(float a, float b)
            => a == b;

        protected override Fader<float> New() => new FloatFader();

    }

    public sealed class ColorFader : Fader<Color> {

        public ColorFader()
            : base(false, default) {
        }

        public ColorFader(Color value)
            : base(true, value) {
        }

        public ColorFader(byte r, byte g, byte b, byte a)
            : base(true, new(r, g, b, a)) {
        }

        public ColorFader(Func<Color> value)
            : base(value) {
        }

        public override Color Calculate(Color a, Color b, float t)
            => new(
                (byte) (a.R + (b.R - a.R) * t),
                (byte) (a.G + (b.G - a.G) * t),
                (byte) (a.B + (b.B - a.B) * t),
                (byte) (a.A + (b.A - a.A) * t)
            );

        protected override bool Equal(Color a, Color b)
            => a == b;

        protected override Fader<Color> New() => new ColorFader();

    }
}
