using System.Drawing;

using Circuit.Elements.Gate;

namespace Circuit.UI.Gate {
    class Schmitt : InvertingSchmitt {
        public Schmitt(Point pos) : base(pos) {
            Elm = new ElmSchmitt();
        }

        public Schmitt(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new ElmSchmitt(st);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.SCHMITT; } }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmSchmitt)Elm;
            drawPosts();
            draw2Leads();
            g.DrawColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            g.DrawPolygon(gatePoly);
            g.DrawPolygon(symbolPoly);
            CurCount = updateDotCount(ce.Current, CurCount);
            drawDotsB(CurCount);
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
