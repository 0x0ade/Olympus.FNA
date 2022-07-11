using Microsoft.Xna.Framework;
using System;

namespace OlympUI.Modifiers {
    public sealed class OpacityModifier : Modifier<Element> {

        public Style.KeyOrValue<float> Multiplier;

        public OpacityModifier(Style.KeyOrValue<float> multiplier) {
            Multiplier = multiplier;
        }

        public override void ModifyDraw(ref UICmd.Sprite cmd) {
            cmd.Color *= Element.Style.GetCurrent(Multiplier);
        }

    }
}