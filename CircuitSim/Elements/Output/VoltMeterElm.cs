using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Output {
    class VoltMeterElm : CircuitElm {
        const int FLAG_SHOWVOLTAGE = 1;

        const int TP_VOL = 0;
        const int TP_RMS = 1;
        const int TP_MAX = 2;
        const int TP_MIN = 3;
        const int TP_P2P = 4;
        const int TP_BIN = 5;
        const int TP_FRQ = 6;
        const int TP_PER = 7;
        const int TP_PWI = 8;
        const int TP_DUT = 9; /* mark to space ratio */

        int mMeter;
        E_SCALE mScale;

        double mRmsV = 0;
        double mTotal;
        double mCount;
        double mBinaryLevel = 0; /*0 or 1 - double because we only pass doubles back to the web page */
        int mZeroCount = 0;
        double mMaxV = 0, mLastMaxV;
        double mMinV = 0, mLastMinV;
        double mFrequency = 0;
        double mPeriod = 0;
        double mPulseWidth = 0;
        double mDutyCycle = 0;
        double mSelectedValue = 0;

        bool mIncreasingV = true;
        bool mDecreasingV = true;

        long mPeriodStart; /* time between consecutive max values */
        long mPeriodLength;
        long mPulseStart;

        Point mCenter;
        Point mPlusPoint;

        public VoltMeterElm(Point pos) : base(pos) {
            mMeter = TP_VOL;

            /* default for new elements */
            mFlags = FLAG_SHOWVOLTAGE;
            mScale = E_SCALE.AUTO;
        }

        public VoltMeterElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            mMeter = TP_VOL;
            mScale = E_SCALE.AUTO;
            try {
                mMeter = st.nextTokenInt(); /* get meter type from saved dump */
                mScale = st.nextTokenEnum<E_SCALE>();
            } catch { }
        }

        public override DUMP_ID Shortcut { get { return DUMP_ID.VOLTMETER; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.VOLTMETER; } }

        protected override string dump() {
            return mMeter + " " + mScale;
        }

        public override bool CirGetConnection(int n1, int n2) { return false; }

        public override void CirStepFinished() {
            mCount++; /*how many counts are in a cycle */
            double v = CirVoltageDiff;
            mTotal += v * v;

            if (v < 2.5) {
                mBinaryLevel = 0;
            } else {
                mBinaryLevel = 1;
            }

            /* V going up, track maximum value with */
            if (v > mMaxV && mIncreasingV) {
                mMaxV = v;
                mIncreasingV = true;
                mDecreasingV = false;
            }

            if (v < mMaxV && mIncreasingV) { /* change of direction V now going down - at start of waveform */
                mLastMaxV = mMaxV; /* capture last maximum */
                                   /* capture time between */
                var now = DateTime.Now.ToFileTimeUtc();
                mPeriodLength = now - mPeriodStart;
                mPeriodStart = now;
                mPeriod = mPeriodLength;
                mPulseWidth = now - mPulseStart;
                mDutyCycle = mPulseWidth / mPeriodLength;
                mMinV = v; /* track minimum value with V */
                mIncreasingV = false;
                mDecreasingV = true;

                /* rms data */
                mTotal = mTotal / mCount;
                mRmsV = Math.Sqrt(mTotal);
                if (double.IsNaN(mRmsV)) {
                    mRmsV = 0;
                }
                mCount = 0;
                mTotal = 0;
            }

            if (v < mMinV && mDecreasingV) { /* V going down, track minimum value with V */
                mMinV = v;
                mIncreasingV = false;
                mDecreasingV = true;
            }

            if (v > mMinV && mDecreasingV) { /* change of direction V now going up */
                mLastMinV = mMinV; /* capture last minimum */
                mPulseStart = DateTime.Now.ToFileTimeUtc();
                mMaxV = v;
                mIncreasingV = true;
                mDecreasingV = false;

                /* rms data */
                mTotal = mTotal / mCount;
                mRmsV = Math.Sqrt(mTotal);
                if (double.IsNaN(mRmsV)) {
                    mRmsV = 0;
                }
                mCount = 0;
                mTotal = 0;
            }

            /* need to zero the rms value if it stays at 0 for a while */
            if (v == 0) {
                mZeroCount++;
                if (mZeroCount > 5) {
                    mTotal = 0;
                    mRmsV = 0;
                    mMaxV = 0;
                    mMinV = 0;
                }
            } else {
                mZeroCount = 0;
            }
        }

        public override void SetPoints() {
            base.SetPoints();
            interpPoint(ref mCenter, 0.5, 12 * mDsign);
            interpPoint(ref mPlusPoint, 8.0 / mLen, 6 * mDsign);
        }

        public override void Draw(CustomGraphics g) {
            int hs = 8;
            setBbox(mPoint1, mPoint2, hs);
            bool selected = NeedsHighlight;
            double len = (selected || CirSim.Sim.DragElm == this || mustShowVoltage()) ? 16 : mLen - 32;
            calcLeads((int)len);

            if (selected) {
                g.LineColor = CustomGraphics.SelectColor;
            } else {
                g.LineColor = CustomGraphics.GrayColor;
            }
            drawLead(mPoint1, mLead1);

            if (selected) {
                g.LineColor = CustomGraphics.SelectColor;
            } else {
                g.LineColor = CustomGraphics.GrayColor;
            }
            drawLead(mLead2, mPoint2);

            if (this == CirSim.Sim.PlotXElm) {
                drawCenteredLText("X", mCenter, true);
            }
            if (this == CirSim.Sim.PlotYElm) {
                drawCenteredLText("Y", mCenter, true);
            }

            if (mustShowVoltage()) {
                string s = "";
                switch (mMeter) {
                case TP_VOL:
                    s = Utils.UnitTextWithScale(CirVoltageDiff, "V", mScale);
                    break;
                case TP_RMS:
                    s = Utils.UnitTextWithScale(mRmsV, "V(rms)", mScale);
                    break;
                case TP_MAX:
                    s = Utils.UnitTextWithScale(mLastMaxV, "Vpk", mScale);
                    break;
                case TP_MIN:
                    s = Utils.UnitTextWithScale(mLastMinV, "Vmin", mScale);
                    break;
                case TP_P2P:
                    s = Utils.UnitTextWithScale(mLastMaxV - mLastMinV, "Vp2p", mScale);
                    break;
                case TP_BIN:
                    s = mBinaryLevel + "";
                    break;
                case TP_FRQ:
                    s = Utils.UnitText(mFrequency, "Hz");
                    break;
                case TP_PER:
                    s = "percent:" + mPeriod + " " + ControlPanel.TimeStep + " " + CirSim.Sim.Time + " " + CirSim.Sim.getIterCount();
                    break;
                case TP_PWI:
                    s = Utils.UnitText(mPulseWidth, "S");
                    break;
                case TP_DUT:
                    s = mDutyCycle.ToString("0.000");
                    break;
                }
                drawCenteredText(s, mCenter, true);
            }
            drawCenteredLText("+", mPlusPoint, true);
            drawPosts();
        }

        string getMeter() {
            switch (mMeter) {
            case TP_VOL:
                return "V";
            case TP_RMS:
                return "V(rms)";
            case TP_MAX:
                return "Vmax";
            case TP_MIN:
                return "Vmin";
            case TP_P2P:
                return "Peak to peak";
            case TP_BIN:
                return "Binary";
            case TP_FRQ:
                return "Frequency";
            case TP_PER:
                return "Period";
            case TP_PWI:
                return "Pulse width";
            case TP_DUT:
                return "Duty cycle";
            }
            return "";
        }

        bool mustShowVoltage() {
            return (mFlags & FLAG_SHOWVOLTAGE) != 0;
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "voltmeter";
            arr[1] = "Vd = " + Utils.VoltageText(CirVoltageDiff);
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                var ei = new ElementInfo("表示", mSelectedValue, -1, -1);
                ei.Choice = new ComboBox();
                ei.Choice.Items.Add("瞬時値");
                ei.Choice.Items.Add("実効値");
                ei.Choice.Items.Add("最大値");
                ei.Choice.Items.Add("最小値");
                ei.Choice.Items.Add("P-P");
                ei.Choice.Items.Add("2値");
                ei.Choice.SelectedIndex = mMeter;
                return ei;
            }
            if (n == 1) {
                var ei = new ElementInfo("スケール", 0);
                ei.Choice = new ComboBox();
                ei.Choice.Items.Add("自動");
                ei.Choice.Items.Add("V");
                ei.Choice.Items.Add("mV");
                ei.Choice.Items.Add("uV");
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
