using Microsoft.Xna.Framework;
using Olympus.NativeImpls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlympUI {
    public abstract class UINativeImpl {

        public readonly Game Game;

        public abstract bool IsMouseFocus { get; }

        public abstract bool CaptureMouse { get; set; }

        public abstract Point MouseOffset { get; }

        public UINativeImpl(Game game) {
            Game = game;
        }

    }
}
