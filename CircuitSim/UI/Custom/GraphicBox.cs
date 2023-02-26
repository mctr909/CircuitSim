using System.Drawing;

namespace Circuit.UI.Custom {
    class GraphicBox : Graphic {
        public GraphicBox(Point pos) : base(pos) {
            DumpInfo.SetP2(pos);
            DumpInfo.SetBbox(pos, DumpInfo.P2X, DumpInfo.P2Y);
        }

        public GraphicBox(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
            DumpInfo.SetP2(b);
            DumpInfo.SetBbox(DumpInfo.P1X, DumpInfo.P1Y, DumpInfo.P2X, DumpInfo.P2Y);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.BOX; } }

        public override bool IsCreationFailed {
            get { return DumpInfo.BoxIsCreationFailed; }
        }

        public override double Distance(int x, int y) {
            return DumpInfo.BoxDistance(x, y);
        }

        public override void Drag(Point p) {
            DumpInfo.SetP2(p);
        }

        public override void Draw(CustomGraphics g) {
            g.DrawColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            DumpInfo.SetBbox(DumpInfo.P1X, DumpInfo.P1Y, DumpInfo.P2X, DumpInfo.P2Y);
            var x1 = DumpInfo.P1X;
            var y1 = DumpInfo.P1Y;
            var x2 = DumpInfo.P2X;
            var y2 = DumpInfo.P2Y;
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

        public override ElementInfo GetElementInfo(int r, int c) { return null; }

        public override void SetElementValue(int n, int c, ElementInfo ei) { }
    }
}
