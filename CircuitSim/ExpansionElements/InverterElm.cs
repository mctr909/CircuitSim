using System;
using System.Drawing;

namespace Circuit.Elements {
    class InverterElm : CircuitElm {
        double slewRate; /* V/ns */
        double highVoltage;
        Point[] gatePolyEuro;
        Point[] gatePolyAnsi;
        Point pcircle;
        double lastOutputVoltage;
        Point center;

        public InverterElm(int xx, int yy) : base(xx, yy) {
            mNoDiagonal = true;
            slewRate = .5;

            /* copy defaults from last gate edited */
            highVoltage = GateElm.lastHighVoltage;
        }

        public InverterElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            mNoDiagonal = true;
            try {
                slewRate = st.nextTokenDouble();
                highVoltage = st.nextTokenDouble();
            } catch {
                slewRate = .5;
                highVoltage = 5;
            }
        }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.INVERT; } }

        protected override string dump() { return ""; }

        public override void Draw(CustomGraphics g) {
            drawPosts(g);
            draw2Leads(g);
            g.ThickLineColor = NeedsHighlight ? SelectColor : GrayColor;
            if (GateElm.useAnsiGates()) {
                g.DrawThickPolygon(gatePolyAnsi);
            } else {
                g.DrawThickPolygon(gatePolyEuro);
                drawCenteredLText(g, "1", center.X, center.Y - 6, true);
            }
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

            gatePolyAnsi = new Point[3];
            Utils.InterpPoint(mLead1, mLead2, ref gatePolyAnsi[0], ref gatePolyAnsi[1], 0, hs);
            gatePolyAnsi[2] = Utils.InterpPoint(mPoint1, mPoint2, .5 + (ww - 5) / mLen);

            gatePolyEuro = new Point[4];
            var l2 = Utils.InterpPoint(mPoint1, mPoint2, .5 + (ww - 5) / mLen); /* make room for circle */
            Utils.InterpPoint(mLead1, l2, ref gatePolyEuro[0], ref gatePolyEuro[1], 0, hs);
            Utils.InterpPoint(mLead1, l2, ref gatePolyEuro[3], ref gatePolyEuro[2], 1, hs);
            center = Utils.InterpPoint(mLead1, l2, .5);

            setBbox(mPoint1, mPoint2, hs);
        }

        public override void Stamp() {
            mCir.StampVoltageSource(0, Nodes[1], mVoltSource);
        }

        public override void StartIteration() {
            lastOutputVoltage = Volts[1];
        }

        public override void DoStep() {
            double v = Volts[0] > highVoltage * .5 ? 0 : highVoltage;
            double maxStep = slewRate * Sim.timeStep * 1e9;
            v = Math.Max(Math.Min(lastOutputVoltage + maxStep, v), lastOutputVoltage - maxStep);
            mCir.UpdateVoltageSource(0, Nodes[1], mVoltSource, v);
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "inverter";
            arr[1] = "Vi = " + Utils.VoltageText(Volts[0]);
            arr[2] = "Vo = " + Utils.VoltageText(Volts[1]);
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("Slew Rate (V/ns)", slewRate, 0, 0);
            }
            if (n == 1) {
                return new ElementInfo("High Voltage (V)", highVoltage, 1, 10);
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0) {
                slewRate = ei.Value;
            }
            if (n == 1) {
                highVoltage = GateElm.lastHighVoltage = ei.Value;
            }
        }

        /* there is no current path through the inverter input,
         * but there is an indirect path through the output to ground. */
        public override bool GetConnection(int n1, int n2) { return false; }

        public override bool HasGroundConnection(int n1) { return n1 == 1; }

        public override double GetCurrentIntoNode(int n) {
            if (n == 1) {
                return mCurrent;
            }
            return 0;
        }
    }
}
