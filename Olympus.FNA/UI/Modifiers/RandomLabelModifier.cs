using Microsoft.Xna.Framework;
using System;

namespace OlympUI.Modifiers {
    // Recreation of the random label modifier from SGUI from 2016, as a proof of concept.
    public sealed class RandomLabelModifier : Modifier<Label> {

        private readonly Random Random = new();
        private string? Scramble;
        private char[]? Buffer;

        public override void Attach(Element elem) {
            base.Attach(elem);

            Scramble = Element.Text;
            Buffer = Scramble.ToCharArray();
        }

        public override void Update(float dt) {
            if (Scramble is null || Buffer is null || Random.Next(10) == 0)
                return;

            Buffer[Random.Next(Buffer.Length)] = Scramble[Random.Next(Scramble.Length)];
            Element.Text = new(Buffer);
        }

    }
}