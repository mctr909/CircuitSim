using System.Drawing;

namespace Circuit.Elements.Gate {
    class InvertingSchmittElm : CircuitElm {
        protected Point[] gatePoly;
        protected Point[] symbolPoly;
        Point pcircle;

        double dlt;
        double dut;

        public InvertingSchmittElm(Point pos, int dummy) : base(pos) {
            mNoDiagonal = true;
        }

        public InvertingSchmittElm(Point pos) : base(pos) {
            CirElm = new InvertingSchmittElmE();
            mNoDiagonal = true;
        }

        public InvertingSchmittElm(Point p1, Point p2, int f) : base(p1, p2, f) {
            mNoDiagonal = true;
        }

        public InvertingSchmittElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            CirElm = new InvertingSchmittElmE(st);
            mNoDiagonal = true;
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.INVERT_SCHMITT; } }

        protected override string dump() {
            var ce = (InvertingSchmittElmE)CirElm;
            return ce.SlewRate
                + " " + ce.LowerTrigger
                + " " + ce.UpperTrigger
                + " " + ce.LogicOnLevel
                + " " + ce.LogicOffLevel;
        }

        public override void Draw(CustomGraphics g) {
            var ce = (InvertingSchmittElmE)CirElm;
            drawPosts();
            draw2Leads();
            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            g.DrawPolygon(gatePoly);
            g.DrawPolygon(symbolPoly);
            g.DrawCircle(pcircle, 3);
            ce.CurCount = ce.cirUpdateDotCount(ce.mCurrent, ce.CurCount);
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
            setLead2(0.5 + (ww + 2) / mLen);
            interpPoint(ref pcircle, 0.5 + (ww - 2) / mLen);
            gatePoly = new Point[3];
            interpLeadAB(ref gatePoly[0], ref gatePoly[1], 0, hs);
            interpPoint(ref gatePoly[2], 0.5 + (ww - 5) / mLen);
            Utils.CreateSchmitt(mPoint1, mPoint2, out symbolPoly, 1, .5 - (ww - 9) / mLen);
            setBbox(mPoint1, mPoint2, hs);
        }

        public override void GetInfo(string[] arr) {
            var ce = (InvertingSchmittElmE)CirElm;
            arr[0] = "inverting Schmitt trigger";
            arr[1] = "Vi = " + Utils.VoltageText(ce.Volts[0]);
            arr[2] = "Vo = " + Utils.VoltageText(ce.Volts[1]);
        }

        // there is no current path through the InvertingSchmitt input, but there
        // is an indirect path through the output to ground.
        public override bool GetConnection(int n1, int n2) { return false; }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (InvertingSchmittElmE)CirElm;
            if (n == 0) {
                dlt = ce.LowerTrigger;
                return new ElementInfo("Lower threshold (V)", ce.LowerTrigger, 0.01, 5);
            }
            if (n == 1) {
                dut = ce.UpperTrigger;
                return new ElementInfo("Upper threshold (V)", ce.UpperTrigger, 0.01, 5);
            }
            if (n == 2) {
                return new ElementInfo("Slew Rate (V/ns)", ce.SlewRate, 0, 0);
            }
            if (n == 3) {
                return new ElementInfo("High Voltage (V)", ce.LogicOnLevel, 0, 0);
            }
            if (n == 4) {
                return new ElementInfo("Low Voltage (V)", ce.LogicOffLevel, 0, 0);
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (InvertingSchmittElmE)CirElm;
            if (n == 0) {
                dlt = ei.Value;
            }
            if (n == 1) {
                dut = ei.Value;
            }
            if (n == 2) {
                ce.SlewRate = ei.Value;
            }
            if (n == 3) {
                ce.LogicOnLevel = ei.Value;
            }
            if (n == 4) {
                ce.LogicOffLevel = ei.Value;
            }
            if (dlt > dut) {
                ce.UpperTrigger = dlt;
                ce.LowerTrigger = dut;
            } else {
                ce.UpperTrigger = dut;
                ce.LowerTrigger = dlt;
            }
        }
    }
}
