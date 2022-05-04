using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Gate {
    abstract class GateElm : CircuitElm {
        const int FLAG_SMALL = 1;
        const int FLAG_SCHMITT = 2;

        const int G_WIDTH = 7;
        const int G_WIDTH2 = 14;
        const int G_HEIGHT = 8;
        const int CIRCLE_SIZE = 3;

        public static double LastHighVoltage = 5;

        static bool mLastSchmitt = false;

        protected int mHs2;
        protected int mWw;
        protected int mInputCount = 2;

        protected Point[] mGatePolyEuro;
        protected Point[] mGatePolyAnsi;

        protected Point mCirclePos;
        protected Point[] mLinePoints;

        Point[] mSchmittPoly;
        Point[] mInPosts;
        Point[] mInGates;

        bool mLastOutput;
        double mHighVoltage;
        bool[] mInputStates;
        int mOscillationCount;

        protected abstract string getGateName();

        protected abstract bool calcFunction();

        public GateElm(Point pos) : base(pos) {
            mNoDiagonal = true;
            mInputCount = 2;

            /* copy defaults from last gate edited */
            mHighVoltage = LastHighVoltage;
            if (mLastSchmitt) {
                mFlags |= FLAG_SCHMITT;
            }

            mFlags |= FLAG_SMALL;
        }

        public GateElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            mInputCount = st.nextTokenInt();
            double lastOutputVoltage = st.nextTokenDouble();
            mNoDiagonal = true;
            try {
                mHighVoltage = st.nextTokenDouble();
            } catch {
                mHighVoltage = 5;
            }
            mLastOutput = lastOutputVoltage > mHighVoltage * .5;

            mFlags |= FLAG_SMALL;
        }

        public override int CirVoltageSourceCount { get { return 1; } }

        public override int CirPostCount { get { return mInputCount + 1; } }

        public static bool useAnsiGates() { return ControlPanel.ChkUseAnsiSymbols.Checked; }

        bool hasSchmittInputs() { return (mFlags & FLAG_SCHMITT) != 0; }

        protected virtual bool isInverting() { return false; }

        protected override string dump() {
            return mInputCount + " " + CirVolts[mInputCount] + " " + mHighVoltage;
        }

        public override Point GetPost(int n) {
            if (n == mInputCount) {
                return mPoint2;
            }
            return mInPosts[n];
        }

        /* there is no current path through the gate inputs,
         * but there is an indirect path through the output to ground. */
        public override bool CirGetConnection(int n1, int n2) { return false; }

        public override double CirGetCurrentIntoNode(int n) {
            if (n == mInputCount) {
                return mCirCurrent;
            }
            return 0;
        }

        public override bool CirHasGroundConnection(int n1) {
            return (n1 == mInputCount);
        }

        protected bool getInput(int x) {
            if (!hasSchmittInputs()) {
                return CirVolts[x] > mHighVoltage * .5;
            }
            bool res = CirVolts[x] > mHighVoltage * (mInputStates[x] ? .35 : .55);
            mInputStates[x] = res;
            return res;
        }

        protected void createEuroGatePolygon() {
            mGatePolyEuro = new Point[4];
            interpLeadAB(ref mGatePolyEuro[0], ref mGatePolyEuro[1], 0, mHs2);
            interpLeadAB(ref mGatePolyEuro[3], ref mGatePolyEuro[2], 1, mHs2);
        }

        protected virtual string getGateText() { return null; }

        public override void CirStamp() {
            mCir.StampVoltageSource(0, CirNodes[mInputCount], mCirVoltSource);
        }

        public override void CirDoStep() {
            bool f = calcFunction();
            if (isInverting()) {
                f = !f;
            }

            /* detect oscillation (using same strategy as Atanua) */
            if (mLastOutput == !f) {
                if (mOscillationCount++ > 50) {
                    /* output is oscillating too much, randomly leave output the same */
                    mOscillationCount = 0;
                    if (CirSim.Random.Next(10) > 5) {
                        f = mLastOutput;
                    }
                }
            } else {
                mOscillationCount = 0;
            }
            mLastOutput = f;
            double res = f ? mHighVoltage : 0;
            mCir.UpdateVoltageSource(0, CirNodes[mInputCount], mCirVoltSource, res);
        }

        public override void SetPoints() {
            base.SetPoints();
            mInputStates = new bool[mInputCount];
            int hs = G_HEIGHT;
            int i;
            mWw = G_WIDTH2;
            if (mWw > mLen / 2) {
                mWw = (int)(mLen / 2);
            }
            if (isInverting() && mWw + 8 > mLen / 2) {
                mWw = (int)(mLen / 2 - 8);
            }
            calcLeads(mWw * 2);
            mInPosts = new Point[mInputCount];
            mInGates = new Point[mInputCount];
            cirAllocNodes();
            int i0 = -mInputCount / 2;
            for (i = 0; i != mInputCount; i++, i0++) {
                if (i0 == 0 && (mInputCount & 1) == 0) {
                    i0++;
                }
                interpPoint(ref mInPosts[i], 0, hs * i0);
                interpLead(ref mInGates[i], 0, hs * i0);
                CirVolts[i] = (mLastOutput ^ isInverting()) ? 5 : 0;
            }
            mHs2 = G_WIDTH * (mInputCount / 2 + 1);
            setBbox(mPoint1, mPoint2, mHs2);
            if (hasSchmittInputs()) {
                Utils.CreateSchmitt(mLead1, mLead2, out mSchmittPoly, 1, .47f);
            }
        }

        public override void Draw(CustomGraphics g) {
            for (int i = 0; i != mInputCount; i++) {
                drawLead(mInPosts[i], mInGates[i]);
            }
            drawLead(mLead2, mPoint2);
            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            if (useAnsiGates()) {
                g.DrawPolygon(mGatePolyAnsi);
            } else {
                g.DrawPolygon(mGatePolyEuro);
                var center = new Point();
                interpPoint(ref center, 0.5);
                drawCenteredLText(getGateText(), center, true);
            }
            if (hasSchmittInputs()) {
                g.LineColor = CustomGraphics.WhiteColor;
                g.DrawPolygon(mSchmittPoly);
            }
            if (mLinePoints != null && useAnsiGates()) {
                for (int i = 0; i != mLinePoints.Length - 1; i++) {
                    drawLead(mLinePoints[i], mLinePoints[i + 1]);
                }
            }
            if (isInverting()) {
                g.DrawCircle(mCirclePos, CIRCLE_SIZE);
            }
            mCirCurCount = cirUpdateDotCount(mCirCurrent, mCirCurCount);
            drawDots(mLead2, mPoint2, mCirCurCount);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            arr[0] = getGateName();
            arr[1] = "Vout = " + Utils.VoltageText(CirVolts[mInputCount]);
            arr[2] = "Iout = " + Utils.CurrentText(mCirCurrent);
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("入力数", mInputCount, 1, 8).SetDimensionless();
            }
            if (n == 1) {
                return new ElementInfo("閾値(V)", mHighVoltage, 1, 10);
            }
            if (n == 2) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    Text = "シュミットトリガー",
                    Checked = hasSchmittInputs()
                };
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0 && ei.Value >= 1) {
                mInputCount = (int)ei.Value;
                SetPoints();
            }
            if (n == 1) {
                mHighVoltage = LastHighVoltage = ei.Value;
            }
            if (n == 2) {
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_SCHMITT;
                } else {
                    mFlags &= ~FLAG_SCHMITT;
                }
                mLastSchmitt = hasSchmittInputs();
                SetPoints();
            }
        }
    }
}
