using System;
using System.Drawing;

namespace Circuit.Elements.Gate {
    class TriStateElm : CircuitElm {
        const int BODY_LEN = 32;

        Point point3;
        Point lead3;
        Point[] gatePoly;

        public TriStateElm(Point pos) : base(pos) {
            CirElm = new TriStateElmE();
        }

        public TriStateElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            CirElm = new TriStateElmE(st);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.TRISTATE; } }

        protected override string dump() {
            var ce = (TriStateElmE)CirElm;
            return ce.Ron + " " + ce.Roff;
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(BODY_LEN);
            int hs = BODY_LEN / 2;
            int ww = BODY_LEN / 2;
            if (ww > mLen / 2) {
                ww = (int)(mLen / 2);
            }
            gatePoly = new Point[3];
            interpLeadAB(ref gatePoly[0], ref gatePoly[1], 0, hs);
            interpPoint(ref gatePoly[2], 0.5 + ww / mLen);
            interpPoint(ref point3, 0.5, -hs);
            interpPoint(ref lead3, 0.5, -hs / 2);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (TriStateElmE)CirElm;
            int hs = 16;
            setBbox(mPoint1, mPoint2, hs);

            draw2Leads();

            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            g.DrawPolygon(gatePoly);
            drawLead(point3, lead3);
            ce.CurCount = ce.cirUpdateDotCount(ce.Current, ce.CurCount);
            drawDots(mLead2, mPoint2, ce.CurCount);
            drawPosts();
        }

        public override void Drag(Point pos) {
            pos = CirSim.Sim.SnapGrid(pos);
            if (Math.Abs(P1.X - pos.X) < Math.Abs(P1.Y - pos.Y)) {
                pos.X = P1.X;
            } else {
                pos.Y = P1.Y;
            }
            int q1 = Math.Abs(P1.X - pos.X) + Math.Abs(P1.Y - pos.Y);
            int q2 = (q1 / 2) % CirSim.GRID_SIZE;
            if (q2 != 0) {
                return;
            }
            P2.X = pos.X;
            P2.Y = pos.Y;
            SetPoints();
        }

        public override Point GetPost(int n) {
            return (n == 0) ? mPoint1 : (n == 1) ? mPoint2 : point3;
        }

        public override void GetInfo(string[] arr) {
            var ce = (TriStateElmE)CirElm;
            arr[0] = "tri-state buffer";
            arr[1] = ce.Open ? "open" : "closed";
            arr[2] = "Vd = " + Utils.VoltageAbsText(ce.VoltageDiff);
            arr[3] = "I = " + Utils.CurrentAbsText(ce.Current);
            arr[4] = "Vc = " + Utils.VoltageText(ce.Volts[2]);
        }

        /* there is no current path through the input, but there
         * is an indirect path through the output to ground. */
        public override bool GetConnection(int n1, int n2) {
            return false;
        }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (TriStateElmE)CirElm;
            if (n == 0) {
                return new ElementInfo("オン抵抗(Ω)", ce.Ron, 0, 0);
            }
            if (n == 1) {
                return new ElementInfo("オフ抵抗(Ω)", ce.Roff, 0, 0);
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (TriStateElmE)CirElm;
            if (n == 0 && ei.Value > 0) {
                ce.Ron = ei.Value;
            }
            if (n == 1 && ei.Value > 0) {
                ce.Roff = ei.Value;
            }
        }
    }
}
