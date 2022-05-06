using System.Drawing;

namespace Circuit.Elements.Gate {
    class GateUIOr : GateUI {
        public GateUIOr(Point pos, int dummy) : base(pos) { }

        public GateUIOr(Point pos) : base(pos) {
            Elm = new GateElmOr();
        }

        public GateUIOr(Point p1, Point p2, int f, StringTokenizer st, int dummy) : base(p1, p2, f, st) { }

        public GateUIOr(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
            Elm = new GateElmOr(st);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.OR_GATE; } }

        protected override string gateName { get { return "OR gate"; } }

        protected override string gateText { get { return "\u22651"; } }

        public override void SetPoints() {
            base.SetPoints();
            var ce = (GateElmOr)Elm;
            createEuroGatePolygon();

            /* 0-15  = top curve,
             * 16    = right,
             * 17-32 = bottom curve,
             * 33-39 = left curve */
            mGatePolyAnsi = new Point[40];
            if (this is GateUIXor) {
                mLinePoints = new Point[7];
            }
            for (int i = 0; i != 16; i++) {
                double a = i / 16.0;
                double b = 1 - a * a;
                interpLeadAB(ref mGatePolyAnsi[i], ref mGatePolyAnsi[32 - i], 0.5 + a / 2, b * mHs2);
            }
            double ww2 = (mWw == 0) ? mLen * 2 : mWw * 2;
            for (int i = 0; i != 7; i++) {
                double a = (i - 3) / 3.0;
                double b = 6 * (1 - a * a) - 3;
                interpLead(ref mGatePolyAnsi[33 + i], b / ww2, a * mHs2);
                if (this is GateUIXor) {
                    interpLead(ref mLinePoints[i], (b - 5) / ww2, a * mHs2);
                }
            }
            mGatePolyAnsi[16] = mLead2;

            if (ce.IsInverting) {
                interpPoint(ref mCirclePos, 0.5 + (mWw + 3) / mLen);
                setLead2(0.5 + (mWw + 6) / mLen);
            }
        }
    }
}
