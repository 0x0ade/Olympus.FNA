using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OlympUI {
    public class Group : Element {

        public const int WHFalse = -1;
        public const int WHTrue = -2;
        public const int WHInit = -3;

        public static readonly new Style DefaultStyle = new() {
            { "Padding", 0 },
            { "Spacing", 0 },
        };

        public override Point InnerXY {
            get {
                int padding = Style.GetCurrent<int>("Padding");
                return new(padding, padding);
            }
        }
        public override Point InnerWH {
            get {
                int padding = Style.GetCurrent<int>("Padding");
                Point wh = WH;
                return new(wh.X - padding * 2, wh.Y - padding * 2);
            }
        }

        #region Min, Max, Auto width and height

        public Point MinWH = new(WHFalse, WHFalse);
        public int MinW {
            get => MinWH.X;
            set => MinWH.X = value;
        }
        public int MinH {
            get => MinWH.Y;
            set => MinWH.Y = value;
        }

        public Point MaxWH = new(WHFalse, WHFalse);
        public int MaxW {
            get => MaxWH.X;
            set => MaxWH.X = value;
        }
        public int MaxH {
            get => MaxWH.Y;
            set => MaxWH.Y = value;
        }

        public Point AutoWH = new(WHInit, WHInit);
        public int AutoW {
            get => AutoWH.X;
            set => AutoWH.X = value;
        }
        public int AutoH {
            get => AutoWH.Y;
            set => AutoWH.Y = value;
        }

        public Point ForceWH = new(WHFalse, WHFalse);
        public int ForceW {
            get => ForceWH.X;
            set => ForceWH.X = value;
        }
        public int ForceH {
            get => ForceWH.Y;
            set => ForceWH.Y = value;
        }

        #endregion

        public Group() {
        }

        public virtual void Resize(Point wh) {
            Point manualWH = WH;
            Point autoWH = AutoWH;

            if (autoWH.X == WHInit)
                autoWH.X = manualWH.X == default ? WHTrue : WHFalse;
            if (autoWH.Y == WHInit)
                autoWH.Y = manualWH.Y == default ? WHTrue : WHFalse;

            // The following is mostly ported from the Lua version of Olympus with slight modifications.

            Point forceWH = new();
            if (autoWH.X == WHTrue) {
                forceWH.X = WHFalse;
            } else if (autoWH.X == WHFalse || autoWH.X != manualWH.X) {
                ForceWH.X = forceWH.X = manualWH.X;
            } else {
                forceWH.X = ForceWH.X;
            }
            if (autoWH.Y == WHTrue) {
                forceWH.Y = WHFalse;
            } else if (autoWH.Y == WHFalse || autoWH.Y != manualWH.Y) {
                ForceWH.Y = forceWH.Y = manualWH.Y;
            } else {
                forceWH.Y = ForceWH.Y;
            }

            if (forceWH.X >= 0)
                wh.X = forceWH.X;
            if (forceWH.Y >= 0)
                wh.Y = forceWH.Y;

            Point paddingTL = InnerXY;

            if (wh.X < 0 && wh.Y < 0) {
                foreach (Element child in Children) {
                    Point childMax = child.RealXY.ToPoint() - paddingTL + child.WH;
                    wh.X = Math.Max(wh.X, childMax.X);
                    wh.Y = Math.Max(wh.Y, childMax.Y);
                }

            } else if (wh.X < 0) {
                foreach (Element child in Children) {
                    wh.X = Math.Max(wh.X, (int) child.RealX - paddingTL.X + child.W);
                }

            } else if (wh.Y < 0) {
                foreach (Element child in Children) {
                    wh.Y = Math.Max(wh.Y, (int) child.RealY - paddingTL.Y + child.H);
                }
            }

            Point minWH = MinWH;
            minWH = new(
                Math.Max(minWH.X, paddingTL.X * 2),
                Math.Max(minWH.Y, paddingTL.Y * 2)
            );
            Point maxWH = MaxWH;
            if (minWH.X >= 0 && wh.X < minWH.X)
                wh.X = minWH.X;
            if (maxWH.X >= 0 && maxWH.X < wh.X)
                wh.X = maxWH.X;
            if (minWH.Y >= 0 && wh.Y < minWH.Y)
                wh.Y = minWH.Y;
            if (maxWH.Y >= 0 && maxWH.Y < wh.Y)
                wh.Y = maxWH.Y;

            int padding = Style.GetCurrent<int>("Padding");
            wh.X += padding * 2;
            wh.Y += padding * 2;

            AutoWH = WH = wh;
        }

        public virtual void RepositionChildren() {
            Vector2 padding = InnerXY.ToVector2();
            foreach (Element child in Children) {
                child.RealXY = child.XY + padding;
            }
        }

        [LayoutPass(LayoutPass.Normal, LayoutSubpass.Late)]
        private void LayoutNormalChildren(LayoutEvent e) {
            RepositionChildren();
        }

        [LayoutPass(LayoutPass.Normal, LayoutSubpass.Pre - 1)]
        private void LayoutNormalPost(LayoutEvent e) {
            Resize(new(WHFalse, WHFalse));
        }

    }
}
