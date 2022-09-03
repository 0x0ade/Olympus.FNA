using Microsoft.Xna.Framework;
using System;

namespace OlympUI.Animations {
    public sealed class ScaleInAnimation : Animation<Element> {

        public float Scale;

        public ScaleInAnimation(float scale, float duration = DefaultDuration) : base(duration) {
            Scale = scale;
        }

        public override void ModifyDraw(ref UICmd.Sprite cmd) {
            Center(ref cmd);
            cmd.Scale *= MathHelper.Lerp(Scale, 1f, Value);
        }

    }

    public sealed class ScaleOutAnimation : Animation<Element> {

        public float Scale;

        public ScaleOutAnimation(float scale, float duration = DefaultDuration) : base(duration) {
            Scale = scale;
        }

        public override void ModifyDraw(ref UICmd.Sprite cmd) {
            Center(ref cmd);
            cmd.Scale *= MathHelper.Lerp(1f, Scale, Value);
        }

        protected override void End() {
            // Don't remove automatically.
        }

    }
}