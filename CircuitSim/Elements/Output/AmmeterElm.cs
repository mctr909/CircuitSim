using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Output {
    class AmmeterElm : CircuitElm {
        const int AM_VOL = 0;
        const int AM_RMS = 1;
        const int FLAG_SHOWCURRENT = 1;

        int mMeter;
        E_SCALE mScale;

        int mZeroCount = 0;
        double mRmsI = 0;
        double mTotal;
        double mCount;
        double mMaxI = 0;
        double mLastMaxI;
        double mMinI = 0;
        double mLastMinI;
        double mSelectedValue = 0;

        bool mIncreasingI = true;
        bool mDecreasingI = true;

        Point mMid;
        Point[] mArrowPoly;
        Point mTextPos;

        public AmmeterElm(Point pos) : base(pos) {
            mFlags = FLAG_SHOWCURRENT;
            mScale = E_SCALE.AUTO;
        }

        public AmmeterElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            mMeter = st.nextTokenInt();
            try {
                mScale = st.nextTokenEnum<E_SCALE>();
            } catch {
                mScale = E_SCALE.AUTO;
            }
        }

        public override bool IsWire { get { return true; } }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override double Power { get { return 0; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.AMMETER; } }

        protected override string dump() {
            return mMeter + " " + mScale;
        }

        string getMeter() {
            switch (mMeter) {
            case AM_VOL:
                return "I";
            case AM_RMS:
                return "Irms";
            }
            return "";
        }

        public override void StepFinished() {
            mCount++; /*how many counts are in a cycle */
            mTotal += mCurrent * mCurrent; /* sum of squares */
            if (mCurrent > mMaxI && mIncreasingI) {
                mMaxI = mCurrent;
                mIncreasingI = true;
                mDecreasingI = false;
            }

            if (mCurrent < mMaxI && mIncreasingI) { /* change of direction I now going down - at start of waveform */
                mLastMaxI = mMaxI; /* capture last maximum */
                                 /* capture time between */
                mMinI = mCurrent; /* track minimum value */
                mIncreasingI = false;
                mDecreasingI = true;

                /* rms data */
                mTotal = mTotal / mCount;
                mRmsI = Math.Sqrt(mTotal);
                if (double.IsNaN(mRmsI)) {
                    mRmsI = 0;
                }
                mCount = 0;
                mTotal = 0;

            }

            if (mCurrent < mMinI && mDecreasingI) { /* I going down, track minimum value */
                mMinI = mCurrent;
                mIncreasingI = false;
                mDecreasingI = true;
            }

            if (mCurrent > mMinI && mDecreasingI) { /* change of direction I now going up */
                mLastMinI = mMinI; /* capture last minimum */

                mMaxI = mCurrent;
                mIncreasingI = true;
                mDecreasingI = false;

                /* rms data */
                mTotal = mTotal / mCount;
                mRmsI = Math.Sqrt(mTotal);
                if (double.IsNaN(mRmsI)) {
                    mRmsI = 0;
                }
                mCount = 0;
                mTotal = 0;
            }

            /* need to zero the rms value if it stays at 0 for a while */
            if (mCurrent == 0) {
                mZeroCount++;
                if (mZeroCount > 5) {
                    mTotal = 0;
                    mRmsI = 0;
                    mMaxI = 0;
                    mMinI = 0;
                }
            } else {
                mZeroCount = 0;
            }

            switch (mMeter) {
            case AM_VOL:
                mSelectedValue = mCurrent;
                break;
            case AM_RMS:
                mSelectedValue = mRmsI;
                break;
            }
        }

        public override void Stamp() {
            mCir.StampVoltageSource(Nodes[0], Nodes[1], mVoltSource, 0);
        }

        public override void SetPoints() {
            base.SetPoints();
            interpPoint(ref mMid, 0.5 + 8 / mLen);
            Utils.CreateArrow(mPoint1, mMid, out mArrowPoly, 14, 7);
            int sign;
            if (mPoint1.Y == mPoint2.Y) {
                sign = mDsign;
            } else {
                sign = -mDsign;
            }
            interpPoint(ref mTextPos, 0.5, 10 * sign);
        }

        public override void Draw(CustomGraphics g) {
            base.Draw(g); /* BC required for highlighting */
            var c = getVoltageColor(Volts[0]);
            g.LineColor = c;
            g.DrawLine(mPoint1, mPoint2);
            g.FillPolygon(c, mArrowPoly);
            doDots();
            setBbox(mPoint1, mPoint2, 3);
            string s = "A";
            switch (mMeter) {
            case AM_VOL:
                s = Utils.UnitTextWithScale(mCurrent, "A", mScale);
                break;
            case AM_RMS:
                s = Utils.UnitTextWithScale(mRmsI, "A(rms)", mScale);
                break;
            }
            g.DrawRightText(s, mTextPos.X, mTextPos.Y);
            drawPosts();
        }

        bool mustShowCurrent() {
            return (mFlags & FLAG_SHOWCURRENT) != 0;
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "Ammeter";
            switch (mMeter) {
            case AM_VOL:
                arr[1] = "I = " + Utils.UnitText(mCurrent, "A");
                break;
            case AM_RMS:
                arr[1] = "Irms = " + Utils.UnitText(mRmsI, "A");
                break;
            }
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                var ei = new ElementInfo("表示", mSelectedValue, -1, -1);
                ei.Choice = new ComboBox();
                ei.Choice.Items.Add("瞬時値");
                ei.Choice.Items.Add("実効値");
                ei.Choice.SelectedIndex = mMeter;
                return ei;
            }
            if (n == 1) {
                var ei = new ElementInfo("スケール", 0);
                ei.Choice = new ComboBox();
                ei.Choice.Items.Add("自動");
                ei.Choice.Items.Add("A");
                ei.Choice.Items.Add("mA");
                ei.Choice.Items.Add(CirSim.MU_TEXT + "A");
                ei.Choice.SelectedIndex = (int)mScale;
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0) {
                mMeter = ei.Choice.SelectedIndex;
            }
            if (n == 1) {
                mScale = (E_SCALE)ei.Choice.SelectedIndex;
            }
        }
    }
}
