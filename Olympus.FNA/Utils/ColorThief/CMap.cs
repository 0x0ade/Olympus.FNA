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
    ///     Color map
    /// </summary>
    internal class CMap {

        private readonly List<VBox> vboxes = new();
        private List<QuantizedColor>? palette;

        public void Push(VBox box) {
            palette = null;
            vboxes.Add(box);
        }

        public List<QuantizedColor> GeneratePalette() {
            if (palette is null) {
                palette = (from vBox in vboxes
                           let color = vBox.Avg(false)
                           select new QuantizedColor(color, vBox.Count(false))).ToList();
            }

            return palette;
        }

        public int Size() {
            return vboxes.Count;
        }

        public Color Map(Color color) {
            foreach (VBox vbox in vboxes.Where(vbox => vbox.Contains(color))) {
                return vbox.Avg(false);
            }
            return Nearest(color);
        }

        public Color Nearest(Color color) {
            double max = double.MaxValue;
            Color found = default;

            foreach (VBox t in vboxes) {
                Color test = t.Avg(false);
                double dist = Math.Sqrt(
                    Math.Pow(color.R - test.R, 2) +
                    Math.Pow(color.G - test.G, 2) +
                    Math.Pow(color.B - test.B, 2)
                );
                if (dist < max) {
                    max = dist;
                    found = test;
                }
            }

            return found;
        }

        public VBox FindColor(double targetLuma, double minLuma, double maxLuma, double targetSaturation, double minSaturation, double maxSaturation) {
            VBox max = default;
            double maxValue = 0;
            int highestPopulation = vboxes.Select(p => p.Count(false)).Max();

            foreach (VBox swatch in vboxes) {
                Color avg = swatch.Avg(false);
                HslColor hsl = avg.ToHsl();
                double sat = hsl.S;
                double luma = hsl.L;

                if (sat >= minSaturation && sat <= maxSaturation &&
                   luma >= minLuma && luma <= maxLuma) {
                    double thisValue = Mmcq.CreateComparisonValue(
                        sat, targetSaturation, luma, targetLuma,
                        swatch.Count(false), highestPopulation
                    );

                    if (maxValue == 0 || thisValue > maxValue) {
                        max = swatch;
                        maxValue = thisValue;
                    }
                }
            }

            return max;
        }

    }
}
