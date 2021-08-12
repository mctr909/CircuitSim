using System;
using System.Drawing;

namespace Circuit.LogicElements {
    class AndGateElm : GateElm {
        public AndGateElm(Point pos) : base(pos) { }

        public AndGateElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) { }

        public override DUMP_ID DumpType { get { return DUMP_ID.AND_GATE; } }

        protected override string getGateText() { return "&"; }

        public override void SetPoints() {
            base.SetPoints();

            createEuroGatePolygon();

            /* 0    = topleft,
             * 1-10 = top curve,
             * 11   = right,
             * 12-21= bottom curve,
             * 22   = bottom left */
            gatePolyAnsi = new Point[23];
            Utils.InterpPoint(mLead1, mLead2, ref gatePolyAnsi[0], ref gatePolyAnsi[22], 0, hs2);
            for (int i = 0; i != 10; i++) {
                double a = i * .1;
                double b = Math.Sqrt(1 - a * a);
                Utils.InterpPoint(mLead1, mLead2,
                    ref gatePolyAnsi[i + 1], ref gatePolyAnsi[21 - i],
                    .5 + a / 2, b * hs2);
            }
            gatePolyAnsi[11] = mLead2;

            if (isInverting()) {
                circleSize = 6;
                Utils.InterpPoint(mPoint1, mPoint2, ref circlePos, .5 + (ww + 3) / mLen);
                Utils.InterpPoint(mPoint1, mPoint2, ref mLead2, .5 + (ww + 6) / mLen);
            }
        }

        protected override string getGateName() { return "AND gate"; }

        protected override bool calcFunction() {
            bool f = true;
            for (int i = 0; i != inputCount; i++) {
                f &= getInput(i);
            }
            return f;
        }
    }
}
