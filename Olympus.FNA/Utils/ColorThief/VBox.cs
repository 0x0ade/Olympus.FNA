using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace Olympus.ColorThief {
    /// <summary>
    ///     3D color space box.
    /// </summary>
    internal struct VBox {

        private readonly int[] _Histo;
        private Color? _Avg;
        private int? _Count;
        private int? _Volume;
        public Color C1;
        public Color C2;

        public VBox(Color c1, Color c2, int[] histo) {
            C1 = c1;
            C2 = c2;
            _Histo = histo;
            _Avg = null;
            _Count = null;
            _Volume = null;
        }

        public int Volume(bool force) {
            if (_Volume is null || force) {
                _Volume = (C2.R - C1.R + 1) * (C2.G - C1.G + 1) * (C2.B - C1.B + 1);
            }

            return _Volume.Value;
        }

        public int Count(bool force) {
            if (_Count is null || force) {
                int npix = 0;
                for (int r = C1.R; r <= C2.R; r++) {
                    for (int g = C1.G; g <= C2.B; g++) {
                        for (int b = C1.B; b <= C2.B; b++) {
                            npix += _Histo[Mmcq.GetColorIndex(r, g, b)];
                        }
                    }
                }

                _Count = npix;
            }

            return _Count.Value;
        }

        public VBox Clone() {
            return new VBox(C1, C2, _Histo);
        }

        public Color Avg(bool force) {
            if (_Avg is null || force) {
                int ntot = 0;

                int rsum = 0;
                int gsum = 0;
                int bsum = 0;

                for (int r = C1.R; r <= C2.R; r++) {
                    for (int g = C1.G; g <= C2.B; g++) {
                        for (int b = C1.B; b <= C2.B; b++) {
                            int hval = _Histo[Mmcq.GetColorIndex(r, g, b)];
                            ntot += hval;
                            rsum += (int) (hval * (r + 0.5) * Mmcq.Mult);
                            gsum += (int) (hval * (g + 0.5) * Mmcq.Mult);
                            bsum += (int) (hval * (b + 0.5) * Mmcq.Mult);
                        }
                    }
                }

                if (ntot > 0) {
                    _Avg = new(
                        Math.Abs(rsum / ntot),
                        Math.Abs(gsum / ntot),
                        Math.Abs(bsum / ntot)
                    );
                } else {
                    _Avg = new(
                        Math.Abs(Mmcq.Mult * (C1.R + C2.R + 1) / 2),
                        Math.Abs(Mmcq.Mult * (C1.G + C2.G + 1) / 2),
                        Math.Abs(Mmcq.Mult * (C1.B + C2.B + 1) / 2)
                    );
                }
            }

            return _Avg.Value;
        }

        public bool Contains(Color pixel) {
            int rval = pixel.R >> Mmcq.Rshift;
            int gval = pixel.G >> Mmcq.Rshift;
            int bval = pixel.B >> Mmcq.Rshift;
            return
                rval >= C1.R && rval <= C2.R &&
                gval >= C1.G && gval <= C2.G &&
                bval >= C1.B && bval <= C2.B;
        }

    }

    internal class VBoxCountComparer : IComparer<VBox> {

        public int Compare(VBox x, VBox y) {
            int a = x.Count(false);
            int b = y.Count(false);
            return a < b ? -1 : (a > b ? 1 : 0);
        }

    }

    internal class VBoxComparer : IComparer<VBox> {

        public int Compare(VBox x, VBox y) {
            int aCount = x.Count(false);
            int bCount = y.Count(false);
            int aVolume = x.Volume(false);
            int bVolume = y.Volume(false);

            // Otherwise sort by products
            int a = aCount * aVolume;
            int b = bCount * bVolume;
            return a < b ? -1 : (a > b ? 1 : 0);
        }

    }
}
