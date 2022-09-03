using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace OlympUI {
    public abstract class Animation<TElement> : Modifier<TElement> where TElement : Element {

        public const float DefaultDuration = 0.2f;

        public bool Loop;

        private float _Duration;
        public float Duration {
            get => _Duration;
            set {
                _Duration = value;
                UpdateValue();
            }
        }

        private float _Time;
        public float Time {
            get => _Time;
            set {
                _Time = value;
                UpdateValue();
            }
        }

        private Ease.Easer _Easing = Ease.SineInOut;
        public Ease.Easer Easing {
            get => _Easing;
            set {
                _Easing = value;
                UpdateValue();
            }
        }

        public float Value { get; private set; }

        public Animation(float duration = DefaultDuration) {
            Duration = duration;
        }

        public override void Update(float dt) {
            base.Update(dt);

            _Time += dt;
            if (_Time >= _Duration) {
                if (Loop) {
                    _Time %= _Duration;
                } else {
                    _Time = _Duration;
                }

                End();
            }

            UpdateValue();

            if (Meta.ModifyDraw)
                Element.InvalidatePaint();
        }

        protected virtual void UpdateValue(float value) {
        }

        protected virtual void End() {
            if (!Loop)
                Element.Modifiers.Remove(this);
        }

        private void UpdateValue()
            => UpdateValue(Value = _Easing(_Time / _Duration));

        protected void Center(ref UICmd.Sprite cmd) {
            Vector2 offs = new Vector2(cmd.Source.Width, cmd.Source.Height) * 0.5f - cmd.Origin;
            cmd.Position += offs * cmd.Scale;
            cmd.Origin += offs;
        }

    }

    public static class AnimationExtensions {

        public static Animation<TElement> With<TElement>(this Animation<TElement> animation, Ease.Easer easing) where TElement : Element {
            animation.Easing = easing;
            return animation;
        }

    }
}
