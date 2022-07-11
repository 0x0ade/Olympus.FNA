using Microsoft.Xna.Framework;
using System;

namespace OlympUI.Animations {
    public sealed class FadeInAnimation : Animation<Element> {

        public FadeInAnimation(float duration = DefaultDuration) : base(duration) {
        }

        public override void ModifyDraw(ref UICmd.Sprite cmd) {
            cmd.Color *= Value;
        }

    }

    public sealed class FadeOutAnimation : Animation<Element> {

        public FadeOutAnimation(float duration = DefaultDuration) : base(duration) {
        }

        public override void ModifyDraw(ref UICmd.Sprite cmd) {
            cmd.Color *= 1f - Value;
        }

    }
}