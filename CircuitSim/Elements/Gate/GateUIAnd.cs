using System;
using System.Drawing;

namespace Circuit.Elements.Gate {
    class GateUIAnd : GateUI {
        public GateUIAnd(Point pos) : base(pos) {
            Elm = new GateElmAnd();
        }

        public GateUIAnd(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
            Elm = new GateElmAnd(st);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.AND_GATE; } }

        protected override string gateText { get { return "&"; } }

        protected override string gateName { get { return "AND gate"; } }

        public override void SetPoints() {
            base.SetPoints();
            var ce = (GateElm)Elm;

            createEuroGatePolygon();

            /* 0    = topleft,
             * 1-10 = top curve,
             * 11   = right,
             * 12-21= bottom curve,
             * 22   = bottom left */
            mGatePolyAnsi = new Point[23];
            interpLeadAB(ref mGatePolyAnsi[0], ref mGatePolyAnsi[22], 0, mHs2);
            for (int i = 0; i != 10; i++) {
                double a = i * .1;
                double b = Math.Sqrt(1 - a * a);
                interpLeadAB(ref mGatePolyAnsi[i + 1], ref mGatePolyAnsi[21 - i], 0.5 + a / 2, b * mHs2);
            }
            mGatePolyAnsi[11] = mLead2;

            if (ce.IsInverting) {
                interpPoint(ref mCirclePos, 0.5 + (mWw + 3) / mLen);
                setLead2(0.5 + (mWw + 6) / mLen);
            }
        }
    }
}
