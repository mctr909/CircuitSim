using System;
using System.Drawing;

namespace Circuit.Elements.Custom {
    class BoxUI : GraphicUI {
        public BoxUI(Point pos) : base(pos) {
            P2.X = pos.X;
            P2.Y = pos.Y;
            setBbox(pos, P2);
        }

        public BoxUI(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
            P2.X = b.X;
            P2.Y = b.Y;
            setBbox(P1, P2);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.BOX; } }

        public override bool IsCreationFailed {
            get { return Math.Abs(P2.X - P1.X) < 32 || Math.Abs(P2.Y - P1.Y) < 32; }
        }

        public override double Distance(double x, double y) {
            return Math.Min(
                Utils.DistanceOnLine(P1.X, P1.Y, P2.X, P1.Y, x, y), Math.Min(
                Utils.DistanceOnLine(P2.X, P1.Y, P2.X, P2.Y, x, y), Math.Min(
                Utils.DistanceOnLine(P2.X, P2.Y, P1.X, P2.Y, x, y),
                Utils.DistanceOnLine(P1.X, P2.Y, P1.X, P1.Y, x, y)
            )));
        }

        public override void Drag(Point p) {
            P2 = p;
        }

        public override void Draw(CustomGraphics g) {
            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            setBbox(P1, P2);
            var x1 = P1.X;
            var y1 = P1.Y;
            var x2 = P2.X;
            var y2 = P2.Y;
            if (x1 < x2 && y1 < y2) {
                g.DrawDashRectangle(x1, y1, x2 - x1, y2 - y1);
            } else if (x1 > x2 && y1 < y2) {
                g.DrawDashRectangle(x2, y1, x1 - x2, y2 - y1);
            } else if (x1 < x2 && y1 > y2) {
                g.DrawDashRectangle(x1, y2, x2 - x1, y1 - y2);
            } else {
                g.DrawDashRectangle(x2, y2, x1 - x2, y1 - y2);
            }
        }

        public override void GetInfo(string[] arr) { }

        public override ElementInfo GetElementInfo(int n) { return null; }

        public override void SetElementValue(int n, ElementInfo ei) { }
    }
}
