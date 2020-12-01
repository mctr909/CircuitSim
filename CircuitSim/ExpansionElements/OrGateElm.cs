using System.Drawing;

namespace Circuit.Elements {
    class OrGateElm : GateElm {
        public OrGateElm(int xx, int yy) : base(xx, yy) { }

        public OrGateElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f, st) { }

        protected override string getGateName() { return "OR gate"; }

        public override void setPoints() {
            base.setPoints();

            if (useEuroGates()) {
                createEuroGatePolygon();
                linePoints = null;
            } else {
                // 0-15 = top curve, 16 = right, 17-32=bottom curve,
                // 33-37 = left curve
                var triPoints = new Point[38];
                if (this is XorGateElm) {
                    linePoints = new Point[5];
                }
                for (int i = 0; i != 16; i++) {
                    double a = i / 16.0;
                    double b = 1 - a * a;
                    interpPoint(mLead1, mLead2,
                        ref triPoints[i], ref triPoints[32 - i],
                        .5 + a / 2, b * hs2);
                }
                double ww2 = (ww == 0) ? mElmLen * 2 : ww * 2;
                for (int i = 0; i != 5; i++) {
                    double a = (i - 2) / 2.0;
                    double b = 4 * (1 - a * a) - 2;
                    interpPoint(mLead1, mLead2,
                        ref triPoints[33 + i], b / (ww2), a * hs2);
                    if (this is XorGateElm) {
                        linePoints[i] = interpPoint(mLead1, mLead2, (b - 5) / (ww2), a * hs2);
                    }
                }
                triPoints[16] = mLead2;
                gatePoly = createPolygon(triPoints).ToArray();
            }
            if (isInverting()) {
                pcircle = interpPoint(mPoint1, mPoint2, .5 + (ww + 4) / mElmLen);
                mLead2 = interpPoint(mPoint1, mPoint2, .5 + (ww + 8) / mElmLen);
            }
        }

        string getGateText() { return "\u22651"; }

        protected override bool calcFunction() {
            bool f = false;
            for (int i = 0; i != inputCount; i++) {
                f |= getInput(i);
            }
            return f;
        }

        public override DUMP_ID getDumpType() { return DUMP_ID.OR_GATE; }

        public override DUMP_ID getShortcut() { return DUMP_ID.OR_GATE; }
    }
}
