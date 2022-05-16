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
    /// <summary>
    ///     Defines a color in Hue/Saturation/Lightness (HSL) space.
    /// </summary>
    public struct HslColor {

        /// <summary>
        ///     The Alpha/opacity in 0..1 range.
        /// </summary>
        public double A;

        /// <summary>
        ///     The Hue in 0..360 range.
        /// </summary>
        public double H;

        /// <summary>
        ///     The Lightness in 0..1 range.
        /// </summary>
        public double L;

        /// <summary>
        ///     The Saturation in 0..1 range.
        /// </summary>
        public double S;

    }
}
