using System.Drawing;

namespace Circuit.Elements.Gate {
    class InverterElm : CircuitElm {
        Point[] mGatePolyEuro;
        Point[] mGatePolyAnsi;
        Point mCenter;
        Point mPcircle;

        public InverterElm(Point pos) : base(pos) {
            CirElm = new InverterElmE();
            mNoDiagonal = true;
        }

        public InverterElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            CirElm = new InverterElmE(st);
            mNoDiagonal = true;
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.INVERT; } }

        protected override string dump() { return ""; }

        /* there is no current path through the inverter input,
         * but there is an indirect path through the output to ground. */
        public override bool GetConnection(int n1, int n2) { return false; }

        public override void SetPoints() {
            base.SetPoints();
            int hs = 16;
            int ww = 16;
            if (ww > mLen / 2) {
                ww = (int)(mLen / 2);
            }
            setLead1(0.5 - ww / mLen);
            setLead2(0.5 + (ww + 2) / mLen);
            interpPoint(ref mPcircle, 0.5 + (ww - 2) / mLen);

            mGatePolyAnsi = new Point[3];
            interpLeadAB(ref mGatePolyAnsi[0], ref mGatePolyAnsi[1], 0, hs);
            interpPoint(ref mGatePolyAnsi[2], 0.5 + (ww - 5) / mLen);

            mGatePolyEuro = new Point[4];
            var l2 = new Point();
            interpPoint(ref l2, 0.5 + (ww - 5) / mLen); /* make room for circle */
            Utils.InterpPoint(mLead1, l2, ref mGatePolyEuro[0], ref mGatePolyEuro[1], 0, hs);
            Utils.InterpPoint(mLead1, l2, ref mGatePolyEuro[3], ref mGatePolyEuro[2], 1, hs);
            Utils.InterpPoint(mLead1, l2, ref mCenter, .5);

            setBbox(mPoint1, mPoint2, hs);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (InverterElmE)CirElm;
            drawPosts();
            draw2Leads();
            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            if (GateElm.UseAnsiGates()) {
                g.DrawPolygon(mGatePolyAnsi);
            } else {
                g.DrawPolygon(mGatePolyEuro);
                drawCenteredLText("1", mCenter, true);
            }
            g.DrawCircle(mPcircle, 3);
            ce.CurCount = ce.cirUpdateDotCount(ce.Current, ce.CurCount);
            drawDots(mLead2, mPoint2, ce.CurCount);
        }

        public override void GetInfo(string[] arr) {
            var ce = (InverterElmE)CirElm;
            arr[0] = "inverter";
            arr[1] = "Vi = " + Utils.VoltageText(ce.Volts[0]);
            arr[2] = "Vo = " + Utils.VoltageText(ce.Volts[1]);
        }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (InverterElmE)CirElm;
            if (n == 0) {
                return new ElementInfo("Slew Rate (V/ns)", ce.SlewRate, 0, 0);
            }
            if (n == 1) {
                return new ElementInfo("High Voltage (V)", ce.HighVoltage, 1, 10);
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (InverterElmE)CirElm;
            if (n == 0) {
                ce.SlewRate = ei.Value;
            }
            if (n == 1) {
                ce.HighVoltage = GateElmE.LastHighVoltage = ei.Value;
            }
        }
    }
}
