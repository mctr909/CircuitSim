using System;
using System.Drawing;

namespace Circuit.Elements {
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

        public InvertingSchmittElm(int xx, int yy) : base(xx, yy) {
            mNoDiagonal = true;
            slewRate = .5;
            state = false;
            lowerTrigger = 1.66;
            upperTrigger = 3.33;
            logicOnLevel = 5;
            logicOffLevel = 0;
        }

        public InvertingSchmittElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
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

        public override double VoltageDiff { get { return Volts[0]; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.INVERT_SCHMITT; } }

        protected override string dump() {
            return slewRate
                + " " + lowerTrigger
                + " " + upperTrigger
                + " " + logicOnLevel
                + " " + logicOffLevel;
        }

        public override void Draw(CustomGraphics g) {
            drawPosts(g);
            draw2Leads(g);
            g.LineColor = NeedsHighlight ? SelectColor : GrayColor;
            g.ThickLineColor = g.LineColor;
            g.DrawThickPolygon(gatePoly);
            g.DrawPolygon(symbolPoly);
            g.DrawThickCircle(pcircle, 6);
            mCurCount = updateDotCount(mCurrent, mCurCount);
            drawDots(g, mLead2, mPoint2, mCurCount);
        }

        public override void SetPoints() {
            base.SetPoints();
            int hs = 16;
            int ww = 16;
            if (ww > mLen / 2) {
                ww = (int)(mLen / 2);
            }
            mLead1 = Utils.InterpPoint(mPoint1, mPoint2, .5 - ww / mLen);
            mLead2 = Utils.InterpPoint(mPoint1, mPoint2, .5 + (ww + 2) / mLen);
            pcircle = Utils.InterpPoint(mPoint1, mPoint2, .5 + (ww - 2) / mLen);
            gatePoly = new Point[3];
            Utils.InterpPoint(mLead1, mLead2, ref gatePoly[0], ref gatePoly[1], 0, hs);
            gatePoly[2] = Utils.InterpPoint(mPoint1, mPoint2, .5 + (ww - 5) / mLen);
            symbolPoly = Utils.CreateSchmitt(mPoint1, mPoint2, 1, .5 - (ww - 9) / mLen);
            setBbox(mPoint1, mPoint2, hs);
        }

        public override void Stamp() {
            mCir.StampVoltageSource(0, Nodes[1], mVoltSource);
        }

        public override void DoStep() {
            double v0 = Volts[1];
            double _out;
            if (state) {//Output is high
                if (Volts[0] > upperTrigger)//Input voltage high enough to set output low
                {
                    state = false;
                    _out = logicOffLevel;
                } else {
                    _out = logicOnLevel;
                }
            } else {//Output is low
                if (Volts[0] < lowerTrigger)//Input voltage low enough to set output high
                {
                    state = true;
                    _out = logicOnLevel;
                } else {
                    _out = logicOffLevel;
                }
            }
            double maxStep = slewRate * Sim.timeStep * 1e9;
            _out = Math.Max(Math.Min(v0 + maxStep, _out), v0 - maxStep);
            mCir.UpdateVoltageSource(0, Nodes[1], mVoltSource, _out);
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "inverting Schmitt trigger";
            arr[1] = "Vi = " + Utils.VoltageText(Volts[0]);
            arr[2] = "Vo = " + Utils.VoltageText(Volts[1]);
        }

        // there is no current path through the InvertingSchmitt input, but there
        // is an indirect path through the output to ground.
        public override bool GetConnection(int n1, int n2) { return false; }

        public override bool HasGroundConnection(int n1) { return n1 == 1; }

        public override double GetCurrentIntoNode(int n) {
            if (n == 1) {
                return mCurrent;
            }
            return 0;
        }

        public override EditInfo GetEditInfo(int n) {
            if (n == 0) {
                dlt = lowerTrigger;
                return new EditInfo("Lower threshold (V)", lowerTrigger, 0.01, 5);
            }
            if (n == 1) {
                dut = upperTrigger;
                return new EditInfo("Upper threshold (V)", upperTrigger, 0.01, 5);
            }
            if (n == 2) {
                return new EditInfo("Slew Rate (V/ns)", slewRate, 0, 0);
            }
            if (n == 3) {
                return new EditInfo("High Voltage (V)", logicOnLevel, 0, 0);
            }
            if (n == 4) {
                return new EditInfo("Low Voltage (V)", logicOffLevel, 0, 0);
            }
            return null;
        }

        public override void SetEditValue(int n, EditInfo ei) {
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
