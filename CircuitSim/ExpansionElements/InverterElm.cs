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

        protected override string dump() { return ""; }

        protected override DUMP_ID getDumpType() { return DUMP_ID.INVERT; }

        public override void draw(Graphics g) {
            drawPosts(g);
            draw2Leads(g);
            PenThickLine.Color = needsHighlight() ? SelectColor : LightGrayColor;
            if (GateElm.useAnsiGates()) {
                drawThickPolygon(g, gatePolyAnsi);
            } else {
                drawThickPolygon(g, gatePolyEuro);
                drawCenteredText(g, "1", center.X, center.Y - 6, true);
            }
            drawThickCircle(g, pcircle.X, pcircle.Y, 7);
            mCurCount = updateDotCount(mCurrent, mCurCount);
            drawDots(g, mLead2, mPoint2, mCurCount);
        }

        public override void setPoints() {
            base.setPoints();
            int hs = 16;
            int ww = 16;
            if (ww > mElmLen / 2) {
                ww = (int)(mElmLen / 2);
            }
            mLead1 = interpPoint(mPoint1, mPoint2, .5 - ww / mElmLen);
            mLead2 = interpPoint(mPoint1, mPoint2, .5 + (ww + 2) / mElmLen);
            pcircle = interpPoint(mPoint1, mPoint2, .5 + (ww - 2) / mElmLen);

            var triPoints = new Point[3];
            interpPoint(mLead1, mLead2, ref triPoints[0], ref triPoints[1], 0, hs);
            triPoints[2] = interpPoint(mPoint1, mPoint2, .5 + (ww - 5) / mElmLen);
            gatePolyAnsi = createPolygon(triPoints).ToArray();

            var pts = new Point[4];
            var l2 = interpPoint(mPoint1, mPoint2, .5 + (ww - 5) / mElmLen); /* make room for circle */
            interpPoint(mLead1, l2, ref pts[0], ref pts[1], 0, hs);
            interpPoint(mLead1, l2, ref pts[3], ref pts[2], 1, hs);
            gatePolyEuro = createPolygon(pts).ToArray();
            center = interpPoint(mLead1, l2, .5);

            setBbox(mPoint1, mPoint2, hs);
        }

        public override int getVoltageSourceCount() { return 1; }

        public override void stamp() {
            Cir.StampVoltageSource(0, Nodes[1], mVoltSource);
        }

        public override void startIteration() {
            lastOutputVoltage = Volts[1];
        }

        public override void doStep() {
            double v = Volts[0] > highVoltage * .5 ? 0 : highVoltage;
            double maxStep = slewRate * Sim.timeStep * 1e9;
            v = Math.Max(Math.Min(lastOutputVoltage + maxStep, v), lastOutputVoltage - maxStep);
            Cir.UpdateVoltageSource(0, Nodes[1], mVoltSource, v);
        }

        public override double getVoltageDiff() { return Volts[0]; }

        public override void getInfo(string[] arr) {
            arr[0] = "inverter";
            arr[1] = "Vi = " + getVoltageText(Volts[0]);
            arr[2] = "Vo = " + getVoltageText(Volts[1]);
        }

        public override EditInfo getEditInfo(int n) {
            if (n == 0) {
                return new EditInfo("Slew Rate (V/ns)", slewRate, 0, 0);
            }
            if (n == 1) {
                return new EditInfo("High Voltage (V)", highVoltage, 1, 10);
            }
            return null;
        }

        public override void setEditValue(int n, EditInfo ei) {
            if (n == 0) {
                slewRate = ei.Value;
            }
            if (n == 1) {
                highVoltage = GateElm.lastHighVoltage = ei.Value;
            }
        }

        /* there is no current path through the inverter input,
         * but there is an indirect path through the output to ground. */
        public override bool getConnection(int n1, int n2) { return false; }

        public override bool hasGroundConnection(int n1) { return n1 == 1; }

        public override DUMP_ID getShortcut() { return DUMP_ID.INVERT; }

        public override double getCurrentIntoNode(int n) {
            if (n == 1) {
                return mCurrent;
            }
            return 0;
        }
    }
}
