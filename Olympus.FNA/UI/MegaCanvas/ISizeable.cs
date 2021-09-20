using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlympUI.MegaCanvas {
    public interface ISizeable {

        int Width { get; }
        int Height { get; }
        bool IsDisposed { get; }

    }
}
