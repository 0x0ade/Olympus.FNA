using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using OlympUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Olympus.ColorThief {
    public struct QuantizedColor {

        public Color Color { get; private set; }
        public int Population { get; private set; }
        public bool IsDark { get; private set; }

        public QuantizedColor(Color color, int population) {
            Color = color;
            Population = population;
            IsDark = CalculateYiqLuma(color) < 128;
        }

        public static int CalculateYiqLuma(Color color)
            => (int) Math.Round((299 * color.R + 587 * color.G + 114 * color.B) / 1000f);

    }
}
