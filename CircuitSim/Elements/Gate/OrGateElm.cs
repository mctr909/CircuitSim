﻿using System.Drawing;

namespace Circuit.Elements.Gate {
    class OrGateElm : GateElm {
        public OrGateElm(Point pos) : base(pos) { }

        public OrGateElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) { }

        public override DUMP_ID DumpType { get { return DUMP_ID.OR_GATE; } }

        protected override string getGateName() { return "OR gate"; }

        public override void SetPoints() {
            base.SetPoints();

            createEuroGatePolygon();

            /* 0-15  = top curve,
             * 16    = right,
             * 17-32 = bottom curve,
             * 33-39 = left curve */
            gatePolyAnsi = new Point[40];
            if (this is XorGateElm) {
                linePoints = new Point[7];
            }
            for (int i = 0; i != 16; i++) {
                double a = i / 16.0;
                double b = 1 - a * a;
                interpLeadAB(ref gatePolyAnsi[i], ref gatePolyAnsi[32 - i], 0.5 + a / 2, b * hs2);
            }
            double ww2 = (ww == 0) ? mLen * 2 : ww * 2;
            for (int i = 0; i != 7; i++) {
                double a = (i - 3) / 3.0;
                double b = 6 * (1 - a * a) - 3;
                interpLead(ref gatePolyAnsi[33 + i], b / ww2, a * hs2);
                if (this is XorGateElm) {
                    interpLead(ref linePoints[i], (b - 5) / ww2, a * hs2);
                }
            }
            gatePolyAnsi[16] = mLead2;

            if (isInverting()) {
                circleSize = 6;
                interpPoint(ref circlePos, 0.5 + (ww + 3) / mLen);
                interpPoint(ref mLead2, 0.5 + (ww + 6) / mLen);
            }
        }

        protected override string getGateText() { return "\u22651"; }

        protected override bool calcFunction() {
            bool f = false;
            for (int i = 0; i != inputCount; i++) {
                f |= getInput(i);
            }
            return f;
        }
    }
}
