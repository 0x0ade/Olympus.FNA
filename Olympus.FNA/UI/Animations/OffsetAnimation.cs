using Microsoft.Xna.Framework;
using System;

namespace OlympUI.Animations {
    public sealed class OffsetInAnimation : Animation<Element> {

        public Vector2 Offset;

        public OffsetInAnimation(Vector2 offset, float duration = DefaultDuration) : base(duration) {
            Offset = offset;
        }

        public override void ModifyDraw(ref UICmd.Sprite cmd) {
            cmd.Position += Offset * (1f - Value);
        }

    }

    public sealed class OffsetOutAnimation : Animation<Element> {

        public Vector2 Offset;

        public OffsetOutAnimation(Vector2 offset, float duration = DefaultDuration) : base(duration) {
            Offset = offset;
        }

        public override void ModifyDraw(ref UICmd.Sprite cmd) {
            cmd.Position += Offset * Value;
        }

        protected override void End() {
            // Don't remove automatically.
        }

    }
}