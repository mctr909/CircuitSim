using System.Drawing;

namespace Circuit.Elements.Custom {
    class BoxUI : GraphicUI {
        public BoxUI(Point pos) : base(pos) {
            DumpInfo.SetP2(pos);
            DumpInfo.SetBbox(pos, DumpInfo.P2);
        }

        public BoxUI(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
            DumpInfo.SetP2(b);
            DumpInfo.SetBbox(DumpInfo.P1, DumpInfo.P2);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.BOX; } }

        public override bool IsCreationFailed {
            get { return DumpInfo.BoxIsCreationFailed; }
        }

        public override double Distance(int x, int y) {
            return DumpInfo.BoxDistance(x, y);
        }

        public override void Drag(Point p) {
            DumpInfo.P2 = p;
        }

        public override void Draw(CustomGraphics g) {
            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            DumpInfo.SetBbox(DumpInfo.P1, DumpInfo.P2);
            var x1 = DumpInfo.P1.X;
            var y1 = DumpInfo.P1.Y;
            var x2 = DumpInfo.P2.X;
            var y2 = DumpInfo.P2.Y;
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
