using System;
using System.Drawing;

namespace Circuit.UI.Custom {
    class GraphicBox : Graphic {
        public GraphicBox(Point pos) : base(pos) {
            DumpInfo.SetP2(pos);
            DumpInfo.SetBbox(pos, DumpInfo.P2);
        }

        public GraphicBox(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
            DumpInfo.SetP2(b);
            DumpInfo.SetBbox(DumpInfo.P1, DumpInfo.P2);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.BOX; } }

        public override bool IsCreationFailed {
            get { return DumpInfo.BoxIsCreationFailed; }
        }

        public override double Distance(int x, int y) {
            var x1 = DumpInfo.P1.X;
            var y1 = DumpInfo.P1.Y;
            var x2 = DumpInfo.P2.X;
            var y2 = DumpInfo.P2.Y;
            DumpInfo.BoundingBox.Width = 0;
            DumpInfo.BoundingBox.Height = 0;
            return Math.Min(
                Utils.DistanceOnLine(x1, y1, x2, y1, x, y), Math.Min(
                Utils.DistanceOnLine(x1, y1, x1, y2, x, y), Math.Min(
                Utils.DistanceOnLine(x2, y2, x2, y1, x, y),
                Utils.DistanceOnLine(x2, y2, x1, y2, x, y)
            )));
        }

        public override void Drag(Point p) {
            DumpInfo.P2 = CirSimForm.SnapGrid(p);
            DumpInfo.SetBbox(DumpInfo.P1, DumpInfo.P2);
        }

        public override void Draw(CustomGraphics g) {
            var x1 = DumpInfo.P1.X;
            var y1 = DumpInfo.P1.Y;
            var x2 = DumpInfo.P2.X;
            var y2 = DumpInfo.P2.Y;
            if (x1 < x2 && y1 < y2) {
                drawDashRectangle(x1, y1, x2 - x1, y2 - y1);
            } else if (x1 > x2 && y1 < y2) {
                drawDashRectangle(x2, y1, x1 - x2, y2 - y1);
            } else if (x1 < x2 && y1 > y2) {
                drawDashRectangle(x1, y2, x2 - x1, y1 - y2);
            } else {
                drawDashRectangle(x2, y2, x1 - x2, y1 - y2);
            }
            if (NeedsHighlight) {
                Context.DrawPost(DumpInfo.P1);
                Context.DrawPost(DumpInfo.P2);
            }
        }

        public override void GetInfo(string[] arr) { }

        public override ElementInfo GetElementInfo(int r, int c) { return null; }

        public override void SetElementValue(int n, int c, ElementInfo ei) { }
    }
}
