using Microsoft.Xna.Framework;

namespace OlympUI {
    public abstract class UINativeImpl {

        public abstract Game Game { get; }

        public abstract bool IsMouseFocus { get; }

        public abstract bool CaptureMouse { get; set; }

        public abstract Point MouseOffset { get; }

    }
}
