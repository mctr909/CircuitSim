using System;
using System.Drawing;

namespace Circuit.Elements.Logic {
    class InvertingSchmittElm : CircuitElm {
        protected double slewRate; // V/ns
        protected double lowerTrigger;
        protected double upperTrigger;
        protected bool state;
        protected double logicOnLevel;
        protected double logicOffLevel;

        protected Point[] gatePoly;
        protected Point[] symbolPoly;
        Point pcircle;

        double dlt;
        double dut;

        public InvertingSchmittElm(Point pos) : base(pos) {
            mNoDiagonal = true;
            slewRate = .5;
            state = false;
            lowerTrigger = 1.66;
            upperTrigger = 3.33;
            logicOnLevel = 5;
            logicOffLevel = 0;
        }

        public InvertingSchmittElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            mNoDiagonal = true;
            try {
                slewRate = st.nextTokenDouble();
                lowerTrigger = st.nextTokenDouble();
                upperTrigger = st.nextTokenDouble();
                logicOnLevel = st.nextTokenDouble();
                logicOffLevel = st.nextTokenDouble();
            } catch {
                slewRate = .5;
                lowerTrigger = 1.66;
                upperTrigger = 3.33;
                logicOnLevel = 5;
                logicOffLevel = 0;
            }
        }

        public override double CirVoltageDiff { get { return CirVolts[0]; } }

        public override int CirVoltageSourceCount { get { return 1; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.INVERT_SCHMITT; } }

        protected override string dump() {
            return slewRate
                + " " + lowerTrigger
                + " " + upperTrigger
                + " " + logicOnLevel
                + " " + logicOffLevel;
        }

        public override void Draw(CustomGraphics g) {
            drawPosts();
            draw2Leads();
            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            g.DrawPolygon(gatePoly);
            g.DrawPolygon(symbolPoly);
            g.DrawCircle(pcircle, 3);
            mCirCurCount = cirUpdateDotCount(mCirCurrent, mCirCurCount);
            drawDots(mLead2, mPoint2, mCirCurCount);
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

        public override void CirStamp() {
            mCir.StampVoltageSource(0, CirNodes[1], mCirVoltSource);
        }

        public override void CirDoStep() {
            double v0 = CirVolts[1];
            double _out;
            if (state) {//Output is high
                if (CirVolts[0] > upperTrigger)//Input voltage high enough to set output low
                {
                    state = false;
                    _out = logicOffLevel;
                } else {
                    _out = logicOnLevel;
                }
            } else {//Output is low
                if (CirVolts[0] < lowerTrigger)//Input voltage low enough to set output high
                {
                    state = true;
                    _out = logicOnLevel;
                } else {
                    _out = logicOffLevel;
                }
            }
            double maxStep = slewRate * ControlPanel.TimeStep * 1e9;
            _out = Math.Max(Math.Min(v0 + maxStep, _out), v0 - maxStep);
            mCir.UpdateVoltageSource(0, CirNodes[1], mCirVoltSource, _out);
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "inverting Schmitt trigger";
            arr[1] = "Vi = " + Utils.VoltageText(CirVolts[0]);
            arr[2] = "Vo = " + Utils.VoltageText(CirVolts[1]);
        }

        // there is no current path through the InvertingSchmitt input, but there
        // is an indirect path through the output to ground.
        public override bool CirGetConnection(int n1, int n2) { return false; }

        public override bool CirHasGroundConnection(int n1) { return n1 == 1; }

        public override double CirGetCurrentIntoNode(int n) {
            if (n == 1) {
                return mCirCurrent;
            }
            return 0;
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                dlt = lowerTrigger;
                return new ElementInfo("Lower threshold (V)", lowerTrigger, 0.01, 5);
            }
            if (n == 1) {
                dut = upperTrigger;
                return new ElementInfo("Upper threshold (V)", upperTrigger, 0.01, 5);
            }
            if (n == 2) {
                return new ElementInfo("Slew Rate (V/ns)", slewRate, 0, 0);
            }
            if (n == 3) {
                return new ElementInfo("High Voltage (V)", logicOnLevel, 0, 0);
            }
            if (n == 4) {
                return new ElementInfo("Low Voltage (V)", logicOffLevel, 0, 0);
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0) {
                dlt = ei.Value;
            }
            if (n == 1) {
                dut = ei.Value;
            }
            if (n == 2) {
                slewRate = ei.Value;
            }
            if (n == 3) {
                logicOnLevel = ei.Value;
            }
            if (n == 4) {
                logicOffLevel = ei.Value;
            }
            if (dlt > dut) {
                upperTrigger = dlt;
                lowerTrigger = dut;
            } else {
                upperTrigger = dut;
                lowerTrigger = dlt;
            }
        }
    }
}
