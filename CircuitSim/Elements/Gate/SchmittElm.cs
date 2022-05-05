using System.Drawing;

namespace Circuit.Elements.Gate {
    class SchmittElm : InvertingSchmittElm {
        public SchmittElm(Point pos) : base(pos) {
            CirElm = new SchmittElmE();
        }

        public SchmittElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            CirElm = new SchmittElmE(st);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.SCHMITT; } }

        public override void Draw(CustomGraphics g) {
            var ce = (SchmittElmE)CirElm;
            drawPosts();
            draw2Leads();
            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            g.DrawPolygon(gatePoly);
            g.DrawPolygon(symbolPoly);
            ce.CurCount = ce.cirUpdateDotCount(ce.Current, ce.CurCount);
            drawDots(mLead2, mPoint2, ce.CurCount);
        }

        public override void SetPoints() {
            base.SetPoints();
            int hs = 16;
            int ww = 16;
            if (ww > mLen / 2) {
                ww = (int)(mLen / 2);
            }
            setLead1(0.5 - ww / mLen);
            setLead2(0.5 + (ww - 4) / mLen);
            gatePoly = new Point[3];
            interpLeadAB(ref gatePoly[0], ref gatePoly[1], 0, hs);
            interpPoint(ref gatePoly[2], 0.5 + (ww - 5) / mLen);
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "Schmitt Trigger~"; // ~ is for localization
        }
    }
}
