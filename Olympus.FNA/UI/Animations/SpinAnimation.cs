using Microsoft.Xna.Framework;
using System;

namespace OlympUI.Animations {
    public sealed class SpinAnimation : Animation<Element> {

        public SpinAnimation(float duration = DefaultDuration) : base(duration) {
        }

        public override void ModifyDraw(ref UICmd.Sprite cmd) {
            Center(ref cmd);
            cmd.Rotation += Value * MathF.PI * 2f;
        }

    }
}