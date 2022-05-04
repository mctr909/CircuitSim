using System;
using System.Drawing;

namespace Circuit.Elements.Gate {
    class InverterElm : CircuitElm {
        double mSlewRate; /* V/ns */
        double mHighVoltage;
        double mLastOutputVoltage;
        Point[] mGatePolyEuro;
        Point[] mGatePolyAnsi;
        Point mCenter;
        Point mPcircle;

        public InverterElm(Point pos) : base(pos) {
            mNoDiagonal = true;
            mSlewRate = .5;

            /* copy defaults from last gate edited */
            mHighVoltage = GateElm.LastHighVoltage;
        }

        public InverterElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            mNoDiagonal = true;
            try {
                mSlewRate = st.nextTokenDouble();
                mHighVoltage = st.nextTokenDouble();
            } catch {
                mSlewRate = .5;
                mHighVoltage = 5;
            }
        }

        public override double CirVoltageDiff { get { return CirVolts[0]; } }

        public override int CirVoltageSourceCount { get { return 1; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.INVERT; } }

        protected override string dump() { return ""; }

        public override bool CirHasGroundConnection(int n1) { return n1 == 1; }

        /* there is no current path through the inverter input,
         * but there is an indirect path through the output to ground. */
        public override bool CirGetConnection(int n1, int n2) { return false; }

        public override double CirGetCurrentIntoNode(int n) {
            if (n == 1) {
                return mCirCurrent;
            }
            return 0;
        }

        public override void CirStamp() {
            mCir.StampVoltageSource(0, CirNodes[1], mCirVoltSource);
        }

        public override void CirStartIteration() {
            mLastOutputVoltage = CirVolts[1];
        }

        public override void CirDoStep() {
            double v = CirVolts[0] > mHighVoltage * .5 ? 0 : mHighVoltage;
            double maxStep = mSlewRate * ControlPanel.TimeStep * 1e9;
            v = Math.Max(Math.Min(mLastOutputVoltage + maxStep, v), mLastOutputVoltage - maxStep);
            mCir.UpdateVoltageSource(0, CirNodes[1], mCirVoltSource, v);
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
            drawPosts();
            draw2Leads();
            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            if (GateElm.useAnsiGates()) {
                g.DrawPolygon(mGatePolyAnsi);
            } else {
                g.DrawPolygon(mGatePolyEuro);
                drawCenteredLText("1", mCenter, true);
            }
            g.DrawCircle(mPcircle, 3);
            mCirCurCount = cirUpdateDotCount(mCirCurrent, mCirCurCount);
            drawDots(mLead2, mPoint2, mCirCurCount);
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "inverter";
            arr[1] = "Vi = " + Utils.VoltageText(CirVolts[0]);
            arr[2] = "Vo = " + Utils.VoltageText(CirVolts[1]);
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("Slew Rate (V/ns)", mSlewRate, 0, 0);
            }
            if (n == 1) {
                return new ElementInfo("High Voltage (V)", mHighVoltage, 1, 10);
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0) {
                mSlewRate = ei.Value;
            }
            if (n == 1) {
                mHighVoltage = GateElm.LastHighVoltage = ei.Value;
            }
        }
    }
}
