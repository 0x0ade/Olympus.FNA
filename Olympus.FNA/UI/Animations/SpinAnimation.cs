using Microsoft.Xna.Framework;
using System;

namespace OlympUI.Animations {
    public sealed class SpinAnimation : Animation<Element> {

        public SpinAnimation(float duration = DefaultDuration) : base(duration) {
        }

        public override void ModifyDraw(ref UICmd.Sprite cmd) {
            Vector2 offs = Element.WH.ToVector2() * 0.5f - cmd.Origin;
            cmd.Position += offs;
            cmd.Origin += offs;

            cmd.Rotation = Value * MathF.PI * 2f;
        }

    }
}