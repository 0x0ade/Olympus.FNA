using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlympUI.MegaCanvas {
    public sealed class AtlasPage : IDisposable {

        public readonly CanvasManager Manager;
        public readonly GraphicsDevice Graphics;

        public readonly RenderTarget2D RT;

        public readonly List<Rectangle> Spaces = new();
        public readonly HashSet<RenderTarget2DRegion> Taken = new();

        public int Reclaimed = 0;
        public int ReclaimedPrev = 0;
        public int ReclaimedFrames = 0;

        public AtlasPage(CanvasManager manager) {
            Manager = manager;
            Graphics = manager.Graphics;

            RT = new(Graphics, Manager.PageSize, Manager.PageSize, false, SurfaceFormat.Color, DepthFormat.None, Manager.MultiSampleCount, RenderTargetUsage.PreserveContents);
            Spaces.Add(new(0, 0, RT.Width, RT.Height));
        }

        public void Dispose() {
            RT.Dispose();
        }

        public void Update() {
            if (Reclaimed >= 16 || (Reclaimed >= 8 && Reclaimed == ReclaimedPrev)) {
                ReclaimedFrames++;
            } else {
                ReclaimedFrames = 0;
                ReclaimedPrev = Reclaimed;
            }

            if (ReclaimedFrames >= 30 || Reclaimed >= 32) {
                Cleanup();
            }
        }

        public RenderTarget2DRegion? GetRegion(Rectangle want) {
            Rectangle space = default;
            int index = -1;
            for (int i = Spaces.Count - 1; i >= 0; --i) {
                Rectangle entry = Spaces[i];
                if (want.Width <= entry.Width && want.Height <= entry.Height && (
                        index == -1 ||
                        (entry.Width < space.Width && entry.Height < space.Height) ||
                        (want.Width < want.Height ? entry.Width <= space.Width : entry.Height <= space.Height)
                )) {
                    space = entry;
                    index = i;
                    if (want.Width == entry.Width && want.Height == entry.Height)
                        break;
                }
            }

            if (index == -1)
                return null;

            Rectangle taken = new(space.X, space.Y, want.Width, want.Height);
            RenderTarget2DRegion rtrg = new(this, RT, taken);
            bool full = true;
            if (taken.Width < taken.Height) {
                /*
                    +-----------+-----------+
                    |taken      |taken.r    |
                    |           |space.y    |
                    |           |s.r - t.r  |
                    |           |space.h    |
                    +-----------+           |
                    |space.x    |           |
                    |taken.b    |           |
                    |taken.w    |           |
                    |s.b - t.b  |           |
                    |           |           |
                    +-----------------------+
                 */
                if (taken.Right < space.Right) {
                    Rectangle free = new(taken.Right, space.Y, space.Right - taken.Right, space.Height);
                    if (full) {
                        full = false;
                        Spaces[index] = free;
                    } else {
                        Spaces.Add(free);
                    }
                }
                if (taken.Bottom < space.Bottom) {
                    Rectangle free = new(space.X, taken.Bottom, taken.Width, space.Bottom - taken.Bottom);
                    if (full) {
                        full = false;
                        Spaces[index] = free;
                    } else {
                        Spaces.Add(free);
                    }
                }

            } else {
                /*
                    +-----------+-----------+
                    |taken      |taken.r    |
                    |           |space.y    |
                    |           |s.r - t.r  |
                    |           |taken.h    |
                    +-----------+-----------+
                    |space.x                |
                    |taken.b                |
                    |space.w                |
                    |s.b - t.b              |
                    |                       |
                    +-----------------------+
                 */
                if (taken.Right < space.Right) {
                    Rectangle free = new(taken.Right, space.Y, space.Right - taken.Right, taken.Height);
                    if (full) {
                        full = false;
                        Spaces[index] = free;
                    } else {
                        Spaces.Add(free);
                    }
                }
                if (taken.Bottom < space.Bottom) {
                    Rectangle free = new(space.X, taken.Bottom, space.Width, space.Bottom - taken.Bottom);
                    if (full) {
                        full = false;
                        Spaces[index] = free;
                    } else {
                        Spaces.Add(free);
                    }
                }
            }

            if (full)
                Spaces.RemoveAt(index);

            Taken.Add(rtrg);
            return rtrg;
        }

        public void Free(RenderTarget2DRegion? rtrg) {
            if (rtrg == null)
                return;
            Taken.Remove(rtrg);
            Spaces.Add(rtrg.Region);
            Reclaimed++;
        }

        public void Cleanup() {
            // FIXME: Cleanup pages!
            Spaces.Clear();
            Spaces.Add(new(0, 0, RT.Width, RT.Height));

            foreach (RenderTarget2DRegion rtrg in Taken) {
                for (int i = Spaces.Count - 1; i >= 0; --i) {
                    Rectangle space = Spaces[i];

                    // Rectangle subtraction ported from Lua, first implemented by Lönn.

                    Rectangle r1 = space;
                    Rectangle r2 = rtrg.Region;
                    int tlx = Math.Max(r1.X, r2.X);
                    int tly = Math.Max(r1.Y, r2.Y);
                    int brx = Math.Max(r1.Right, r2.Right);
                    int bry = Math.Max(r1.Bottom, r2.Bottom);

                    if (tlx >= brx || tly >= bry) {
                        // No intersection.
                        continue;
                    }

                    Spaces.RemoveAt(i);
                    int ii = i;

                    if (r2.Width < r2.Height) {
                        // Large left rectangle
                        if (tlx > r1.X)
                            Spaces.Insert(ii++, new(r1.X, r1.Y, tlx - r1.X, r1.Height));

                    } else {

                    }

                    /*

                    if r2.width < r2.height then

                    -- Large right rectangle
                    if brx < r1.x + r1.width then
                    table.insert(remaining, rect(brx, r1.y, r1.x + r1.width - brx, r1.height))
                    end

                    -- Small top rectangle
                    if tly > r1.y then
                    table.insert(remaining, rect(tlx, r1.y, brx - tlx, tly - r1.y))
                    end

                    -- Small bottom rectangle
                    if bry < r1.y + r1.height then
                    table.insert(remaining, rect(tlx, bry, brx - tlx, r1.y + r1.height - bry))
                    end

                    else
                    -- Small left rectangle
                    if tlx > r1.x then
                    table.insert(remaining, rect(r1.x, tly, tlx - r1.x, bry - tly))
                    end

                    -- Small right rectangle
                    if brx < r1.x + r1.width then
                    table.insert(remaining, rect(brx, tly, r1.x + r1.width - brx, bry - tly))
                    end

                    -- Large top rectangle
                    if tly > r1.y then
                    table.insert(remaining, rect(r1.x, r1.y, r1.width, tly - r1.y))
                    end

                    -- Large bottom rectangle
                    if bry < r1.y + r1.height then
                    table.insert(remaining, rect(r1.x, bry, r1.width, r1.y + r1.height - bry))
                    end
                    end
                    */
                }
            }
        }

    }
}
