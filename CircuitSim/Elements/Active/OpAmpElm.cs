using System;
using System.Drawing;

namespace Circuit.Elements.Active {
    class OpAmpElm : CircuitElm {
        protected const int FLAG_SWAP = 1;
        protected const int FLAG_SMALL = 2;
        protected const int FLAG_LOWGAIN = 4;
        protected const int FLAG_GAIN = 8;

        const int V_N = 0;
        const int V_P = 1;
        const int V_O = 2;

        const int mOpHeight = 8;
        const int mOpWidth = 16;

        double mMaxOut;
        double mMinOut;
        double mGain;
        double mGbw;

        double mLastVd;

        Point[] mIn1p;
        Point[] mIn2p;
        Point[] mTextp;
        Point[] mTriangle;

        public OpAmpElm(Point pos) : base(pos) {
            mNoDiagonal = true;
            mMaxOut = 15;
            mMinOut = -15;
            mGbw = 1e6;
            mFlags = FLAG_GAIN; /* need to do this before setSize() */
            mFlags |= FLAG_SMALL;
            mGain = 100000;
        }

        public OpAmpElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            /* GBW has no effect in this version of the simulator,
             * but we retain it to keep the file format the same */
            try {
                mMaxOut = st.nextTokenDouble();
                mMinOut = st.nextTokenDouble();
                mGbw = st.nextTokenDouble();
                Volts[V_N] = st.nextTokenDouble();
                Volts[V_P] = st.nextTokenDouble();
                mGain = st.nextTokenDouble();
            } catch {
                mMaxOut = 15;
                mMinOut = -15;
                mGbw = 1e6;
            }
            mNoDiagonal = true;
            mFlags |= FLAG_SMALL;
            setGain();
        }

        public override double VoltageDiff { get { return Volts[V_O] - Volts[V_P]; } }

        public override double Power { get { return Volts[V_O] * mCurrent; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override bool NonLinear { get { return true; } }

        public override int PostCount { get { return 3; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.OPAMP; } }

        protected override string dump() {
            mFlags |= FLAG_GAIN;
            return mMaxOut
                + " " + mMinOut
                + " " + mGbw
                + " " + Volts[V_N]
                + " " + Volts[V_P]
                + " " + mGain;
        }

        void setGain() {
            if ((mFlags & FLAG_GAIN) != 0) {
                return;
            }
            /* gain of 100000 breaks e-amp-dfdx.txt
             * gain was 1000, but it broke amp-schmitt.txt */
            mGain = ((mFlags & FLAG_LOWGAIN) != 0) ? 1000 : 100000;
        }

        public override void Draw(CustomGraphics g) {
            setBbox(mPoint1, mPoint2, mOpHeight * 2);

            drawVoltage(V_N, mIn1p[0], mIn1p[1]);
            drawVoltage(V_P, mIn2p[0], mIn2p[1]);
            drawVoltage(V_O, mLead2, mPoint2);

            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            g.DrawPolygon(mTriangle);

            drawCenteredLText("-", mTextp[0], true);
            drawCenteredLText("+", mTextp[1], true);
            mCurCount = updateDotCount(mCurrent, mCurCount);
            drawDots(mPoint2, mLead2, mCurCount);
            drawPosts();
        }

        public override void SetPoints() {
            base.SetPoints();
            int ww = mOpWidth;
            if (ww > mLen / 2) {
                ww = (int)(mLen / 2);
            }
            calcLeads(ww * 2);
            int hs = mOpHeight * mDsign;
            if ((mFlags & FLAG_SWAP) != 0) {
                hs = -hs;
            }
            mIn1p = new Point[2];
            mIn2p = new Point[2];
            mTextp = new Point[2];
            interpPointAB(ref mIn1p[0], ref mIn2p[0], 0, hs);
            interpLeadAB(ref mIn1p[1], ref mIn2p[1], 0, hs);
            interpLeadAB(ref mTextp[0], ref mTextp[1], 0.2, hs);
            var tris = new Point[2];
            interpLeadAB(ref tris[0], ref tris[1], 0, hs * 2);
            mTriangle = new Point[] { tris[0], tris[1], mLead2 };
        }

        public override Point GetPost(int n) {
            return (n == 0) ? mIn1p[0] : (n == 1) ? mIn2p[0] : mPoint2;
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "op-amp";
            arr[1] = "V+ = " + Utils.VoltageText(Volts[V_P]);
            arr[2] = "V- = " + Utils.VoltageText(Volts[V_N]);
            /* sometimes the voltage goes slightly outside range,
             * to make convergence easier.  so we hide that here. */
            double vo = Math.Max(Math.Min(Volts[V_O], mMaxOut), mMinOut);
            arr[3] = "Vout = " + Utils.VoltageText(vo);
            arr[4] = "Iout = " + Utils.CurrentText(-mCurrent);
            arr[5] = "range = " + Utils.VoltageText(mMinOut)
                + " to " + Utils.VoltageText(mMaxOut);
        }

        public override void Stamp() {
            int vn = mCir.NodeList.Count + mVoltSource;
            mCir.StampNonLinear(vn);
            mCir.StampMatrix(Nodes[2], vn, 1);
        }

        public override void DoStep() {
            double vd = Volts[V_P] - Volts[V_N];
            if (Math.Abs(mLastVd - vd) > .1) {
                mCir.Converged = false;
            } else if (Volts[V_O] > mMaxOut + .1 || Volts[V_O] < mMinOut - .1) {
                mCir.Converged = false;
            }
            double x = 0;
            int vn = mCir.NodeList.Count + mVoltSource;
            double dx = 0;
            if (vd >= mMaxOut / mGain && (mLastVd >= 0 || CirSim.Random.Next(4) == 1)) {
                dx = 1e-4;
                x = mMaxOut - dx * mMaxOut / mGain;
            } else if (vd <= mMinOut / mGain && (mLastVd <= 0 || CirSim.Random.Next(4) == 1)) {
                dx = 1e-4;
                x = mMinOut - dx * mMinOut / mGain;
            } else {
                dx = mGain;
            }
            /*Console.WriteLine("opamp " + vd + " " + Volts[V_O] + " " + dx + " "  + x + " " + lastvd + " " + Cir.Converged);*/

            /* newton-raphson */
            mCir.StampMatrix(vn, Nodes[0], dx);
            mCir.StampMatrix(vn, Nodes[1], -dx);
            mCir.StampMatrix(vn, Nodes[2], 1);
            mCir.StampRightSide(vn, x);

            mLastVd = vd;
        }

        /* there is no current path through the op-amp inputs,
         * but there is an indirect path through the output to ground. */
        public override bool GetConnection(int n1, int n2) { return false; }

        public override bool HasGroundConnection(int n1) { return n1 == 2; }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("+電源(V)", mMaxOut, 1, 20);
            }
            if (n == 1) {
                return new ElementInfo("-電源(V)", mMinOut, -20, 0);
            }
            if (n == 2) {
                return new ElementInfo("ゲイン(db)", 20 * Math.Log10(mGain), 10, 1000000);
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0) {
                mMaxOut = ei.Value;
            }
            if (n == 1) {
                mMinOut = ei.Value;
            }
            if (n == 2 && ei.Value > 0) {
                mGain = Math.Pow(10.0, ei.Value / 20.0);
            }
        }

        public override double GetCurrentIntoNode(int n) {
            if (n == 2) {
                return -mCurrent;
            }
            return 0;
        }
    }
}
