using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Olympus.ColorThief {
    internal static class Mmcq {

        public const int Sigbits = 5;
        public const int Rshift = 8 - Sigbits;
        public const int Mult = 1 << Rshift;
        public const int Histosize = 1 << (3 * Sigbits);
        public const int VboxLength = 1 << Sigbits;
        public const double FractByPopulation = 0.75;
        public const int MaxIterations = 1000;
        public const double WeightSaturation = 3d;
        public const double WeightLuma = 6d;
        public const double WeightPopulation = 1d;
        private static readonly VBoxComparer ComparatorProduct = new VBoxComparer();
        private static readonly VBoxCountComparer ComparatorCount = new VBoxCountComparer();

        public static int GetColorIndex(int r, int g, int b) {
            return (r << (2 * Sigbits)) + (g << Sigbits) + b;
        }

        /// <summary>
        ///     Gets the histo.
        /// </summary>
        /// <param name="pixels">The pixels.</param>
        /// <returns>Histo (1-d array, giving the number of pixels in each quantized region of color space), or null on error.</returns>
        private static int[] GetHisto(Color[] pixels, int quality) {
            int[] histo = new int[Histosize];

            for (int i = 0; i < pixels.Length; i += quality) {
                Color pixel = pixels[i];
                int rval = pixel.R >> Rshift;
                int gval = pixel.G >> Rshift;
                int bval = pixel.B >> Rshift;
                int index = GetColorIndex(rval, gval, bval);
                histo[index]++;
            }

            return histo;
        }

        private static VBox VboxFromPixels(Color[] pixels, int quality, int[] histo) {
            int rmin = 1000000, rmax = 0;
            int gmin = 1000000, gmax = 0;
            int bmin = 1000000, bmax = 0;

            // find min/max
            for (int i = 0; i < pixels.Length; i += quality) {
                Color pixel = pixels[i];
                int rval = pixel.R >> Rshift;
                int gval = pixel.G >> Rshift;
                int bval = pixel.B >> Rshift;

                if (rval < rmin) {
                    rmin = rval;
                } else if (rval > rmax) {
                    rmax = rval;
                }

                if (gval < gmin) {
                    gmin = gval;
                } else if (gval > gmax) {
                    gmax = gval;
                }

                if (bval < bmin) {
                    bmin = bval;
                } else if (bval > bmax) {
                    bmax = bval;
                }
            }

            return new VBox(
                new(rmin, gmin, bmin),
                new(rmax, gmax, bmax),
                histo
            );
        }

        private static void DoCut(CutColor color, VBox vbox, int[] partialsum, int[] lookaheadsum, int total, out VBox vbox1, out VBox vbox2) {
            int vboxDim1;
            int vboxDim2;

            switch (color) {
                case CutColor.R:
                    vboxDim1 = vbox.C1.R;
                    vboxDim2 = vbox.C2.R;
                    break;
                case CutColor.G:
                    vboxDim1 = vbox.C1.G;
                    vboxDim2 = vbox.C2.G;
                    break;
                default:
                    vboxDim1 = vbox.C1.B;
                    vboxDim2 = vbox.C2.B;
                    break;
            }

            for (int i = vboxDim1; i <= vboxDim2; i++) {
                if (partialsum[i] > total / 2) {
                    vbox1 = vbox.Clone();
                    vbox2 = vbox.Clone();

                    int left = i - vboxDim1;
                    int right = vboxDim2 - i;

                    int d2 =
                        left <= right ? Math.Min(vboxDim2 - 1, Math.Abs(i + right / 2)) :
                        Math.Max(vboxDim1, Math.Abs(Convert.ToInt32(i - 1 - left / 2.0)));

                    // avoid 0-count boxes
                    while (d2 < 0 || partialsum[d2] <= 0) {
                        d2++;
                    }
                    int count2 = lookaheadsum[d2];
                    while (count2 == 0 && d2 > 0 && partialsum[d2 - 1] > 0) {
                        count2 = lookaheadsum[--d2];
                    }

                    // set dimensions
                    switch (color) {
                        case CutColor.R:
                            vbox1.C2.R = (byte) d2;
                            vbox2.C1.R = (byte) (d2 + 1);
                            break;
                        case CutColor.G:
                            vbox1.C2.G = (byte) d2;
                            vbox2.C1.G = (byte) (d2 + 1);
                            break;
                        default:
                            vbox1.C2.B = (byte) d2;
                            vbox2.C1.B = (byte) (d2 + 1);
                            break;
                    }

                    return;
                }
            }

            throw new Exception("VBox can't be cut");
        }

        private static int MedianCutApply(IList<int> histo, VBox vbox, out VBox vbox1, out VBox vbox2) {
            if (vbox.Count(false) == 0) {
                vbox1 = default;
                vbox2 = default;
                return 0;
            }

            if (vbox.Count(false) == 1) {
                vbox1 = vbox.Clone();
                vbox2 = default;
                return 1;
            }

            // only one pixel, no split

            int rw = vbox.C2.R - vbox.C1.R + 1;
            int gw = vbox.C2.G - vbox.C1.G + 1;
            int bw = vbox.C2.B - vbox.C1.B + 1;
            int maxw = Math.Max(Math.Max(rw, gw), bw);

            // Find the partial sum arrays along the selected axis.
            int total = 0;
            int[] partialsum = new int[VboxLength];
            // -1 = not set / 0 = 0
            for (int l = 0; l < VboxLength; l++) {
                partialsum[l] = -1;
            }

            // -1 = not set / 0 = 0
            int[] lookaheadsum = new int[VboxLength];
            for (int l = 0; l < VboxLength; l++) {
                lookaheadsum[l] = -1;
            }

            int i, j, k, sum, index;

            if (maxw == rw) {
                for (i = vbox.C1.R; i <= vbox.C2.R; i++) {
                    sum = 0;
                    for (j = vbox.C1.G; j <= vbox.C2.G; j++) {
                        for (k = vbox.C1.B; k <= vbox.C2.B; k++) {
                            index = GetColorIndex(i, j, k);
                            sum += histo[index];
                        }
                    }
                    total += sum;
                    partialsum[i] = total;
                }
            } else if (maxw == gw) {
                for (i = vbox.C1.G; i <= vbox.C2.G; i++) {
                    sum = 0;
                    for (j = vbox.C1.R; j <= vbox.C2.R; j++) {
                        for (k = vbox.C1.B; k <= vbox.C2.B; k++) {
                            index = GetColorIndex(j, i, k);
                            sum += histo[index];
                        }
                    }
                    total += sum;
                    partialsum[i] = total;
                }
            } else /* if (maxw == bw) */ {
                for (i = vbox.C1.B; i <= vbox.C2.B; i++) {
                    sum = 0;
                    for (j = vbox.C1.R; j <= vbox.C2.R; j++) {
                        for (k = vbox.C1.G; k <= vbox.C2.G; k++) {
                            index = GetColorIndex(j, k, i);
                            sum += histo[index];
                        }
                    }
                    total += sum;
                    partialsum[i] = total;
                }
            }

            for (i = 0; i < VboxLength; i++) {
                if (partialsum[i] != -1) {
                    lookaheadsum[i] = total - partialsum[i];
                }
            }

            // determine the cut planes
            if (maxw == rw) {
                DoCut(CutColor.R, vbox, partialsum, lookaheadsum, total, out vbox1, out vbox2);
            } else if (maxw == gw) {
                DoCut(CutColor.G, vbox, partialsum, lookaheadsum, total, out vbox1, out vbox2);
            } else {
                DoCut(CutColor.B, vbox, partialsum, lookaheadsum, total, out vbox1, out vbox2);
            }
            return 2;
        }

        /// <summary>
        ///     Inner function to do the iteration.
        /// </summary>
        /// <param name="lh">The lh.</param>
        /// <param name="comparator">The comparator.</param>
        /// <param name="target">The target.</param>
        /// <param name="histo">The histo.</param>
        /// <exception cref="System.Exception">vbox1 not defined; shouldn't happen!</exception>
        private static void Iter(List<VBox> lh, IComparer<VBox> comparator, int target, IList<int> histo) {
            int ncolors = 1;
            int niters = 0;

            while (niters < MaxIterations) {
                VBox vbox = lh[lh.Count - 1];
                if (vbox.Count(false) == 0) {
                    lh.Sort(comparator);
                    niters++;
                    continue;
                }

                lh.RemoveAt(lh.Count - 1);

                // do the cut
                if (MedianCutApply(histo, vbox, out VBox vbox1, out VBox vbox2) == 2) {
                    lh.Add(vbox1);
                    lh.Add(vbox2);
                    ncolors++;
                } else {
                    lh.Add(vbox1);
                }
                lh.Sort(comparator);

                if (ncolors >= target) {
                    return;
                }
                if (niters++ > MaxIterations) {
                    return;
                }
            }
        }

        public static CMap? Quantize(Color[] pixels, int maxcolors, int quality) {
            // short-circuit
            if (pixels.Length == 0 || maxcolors < 2 || maxcolors > 256) {
                return null;
            }

            int[] histo = GetHisto(pixels, quality);

            // get the beginning vbox from the colors
            VBox vbox = VboxFromPixels(pixels, quality, histo);
            List<VBox> pq = new() {
                vbox
            };

            // Round up to have the same behaviour as in JavaScript
            int target = (int) Math.Ceiling(FractByPopulation * maxcolors);

            // first set of colors, sorted by population
            Iter(pq, ComparatorCount, target, histo);

            // Re-sort by the product of pixel occupancy times the size in color
            // space.
            pq.Sort(ComparatorProduct);

            // next set - generate the median cuts using the (npix * vol) sorting.
            Iter(pq, ComparatorProduct, maxcolors - pq.Count, histo);

            // Reverse to put the highest elements first into the color map
            pq.Reverse();

            // calculate the actual colors
            CMap cmap = new();
            foreach (VBox vb in pq) {
                cmap.Push(vb);
            }

            return cmap;
        }

        public static double CreateComparisonValue(double saturation, double targetSaturation, double luma, double targetLuma, int population, int highestPopulation) {
            return WeightedMean(InvertDiff(saturation, targetSaturation), WeightSaturation,
                InvertDiff(luma, targetLuma), WeightLuma,
                population / (double) highestPopulation, WeightPopulation);
        }

        private static double WeightedMean(params double[] values) {
            double sum = 0;
            double sumWeight = 0;

            for (int i = 0; i < values.Length; i += 2) {
                double value = values[i];
                double weight = values[i + 1];

                sum += value * weight;
                sumWeight += weight;
            }

            return sum / sumWeight;
        }

        private static double InvertDiff(double value, double targetValue) {
            return 1 - Math.Abs(value - targetValue);
        }

        private enum CutColor {
            R,
            G,
            B
        }

    }
}
