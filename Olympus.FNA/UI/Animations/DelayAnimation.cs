using Microsoft.Xna.Framework;
using System;

namespace OlympUI.Animations {
    public sealed class DelayAnimation<TElement> : Animation<TElement> where TElement : Element {

        private Animation<TElement>? Delayed;

        private Metadata? _Meta;
        public override Metadata Meta => _Meta ??= (Delayed?.Meta ?? new(Update: true, ModifyDraw: false));

        public DelayAnimation(float duration = DefaultDuration) : base(duration) {
        }

        public DelayAnimation(Animation<TElement> delayed, float duration = DefaultDuration) : base(duration) {
            Delayed = delayed;
        }

        public override void Attach(Element elem) {
            base.Attach(elem);

            Delayed?.Attach(elem);
        }

        public override void Detach() {
            base.Detach();

            Delayed?.Detach();
        }

        public override void Update(float dt) {
            Loop = false;

            base.Update(dt);

            Delayed?.Update(0f);
        }

        public override void ModifyDraw(ref UICmd.Sprite cmd) {
            Delayed?.ModifyDraw(ref cmd);
        }

        protected override void End() {
            if (Delayed is not null) {
                Delayed.Detach();
                Element.Modifiers.Add(Delayed);
            }

            Element.Modifiers.Remove(this);
        }

    }

    public static class DelayAnimationExtensions {

        public static DelayAnimation<TElement> WithDelay<TElement>(this Animation<TElement> delayed, float duration = Animation<TElement>.DefaultDuration) where TElement : Element
            => new DelayAnimation<TElement>(delayed, duration);

    }
}