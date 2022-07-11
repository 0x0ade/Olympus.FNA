using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace OlympUI {
    public abstract class Modifier {

        private static readonly Dictionary<Type, Metadata> _Metadata = new();

        private Metadata? _Meta;
        public Metadata Meta {
            get {
                if (_Meta is not null)
                    return _Meta;

                Type type = GetType();
                if (_Metadata.TryGetValue(type, out _Meta))
                    return _Meta;

                return _Metadata[type] = _Meta = new Metadata(
                    Update: type.GetMethod(nameof(Update), new Type[] { typeof(float) })?.DeclaringType != typeof(Modifier),
                    ModifyDraw: type.GetMethod(nameof(ModifyDraw), new Type[] { typeof(UICmd.Sprite).MakeByRefType() })?.DeclaringType != typeof(Modifier)
                );
            }
        }

        internal Modifier() {
        }

        public virtual void Attach(Element elem) {
        }

        public virtual void Detach(Element elem) {
        }

        public virtual void Update(float dt) {
        }

        public virtual void ModifyDraw(ref UICmd.Sprite cmd) {
        }

        public record Metadata(bool Update, bool ModifyDraw);

    }

    public abstract class Modifier<TElement> : Modifier where TElement : Element {

        private TElement? _Element;
        public TElement Element => _Element ?? throw new Exception("Modifier not attached");

        protected Modifier() {
        }

        public override void Attach(Element elem) {
            _Element = elem as TElement ?? throw new Exception($"Modifier {GetType().Name} expects elements of type {typeof(TElement).Name}");
        }

        public override void Detach(Element elem) {
            _Element = null;
        }

    }
}
