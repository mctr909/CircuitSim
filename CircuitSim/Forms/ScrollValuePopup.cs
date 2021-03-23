using System;
using System.Windows.Forms;

using Circuit.Elements;

namespace Circuit {
    class ScrollValuePopup : Form {
        static readonly double[] E24 = {
            1.0, 1.1, 1.2, 1.3,
            1.5, 1.6, 1.8, 2.0,
            2.2, 2.4, 2.7, 3.0,
            3.3, 3.6, 3.9, 4.3,
            4.7, 5.1, 5.6, 6.2,
            6.8, 7.5, 8.2, 9.1
        };

        double[] mValues;
        int mMinPow = 0;
        int mMaxPow = 1;
        int mNValues;
        int mCurrentIdx;
        int mLastIdx;

        CirSim mSim;
        CircuitElm mMyElm;
        ElementInfo mInfo;
        Panel mPnlV;
        Label mLabels;
        TrackBar mTrbValue;
        int mDeltaY;
        string mName;
        string mUnit;

        public ScrollValuePopup(int dy, CircuitElm e, CirSim s) : base() {
            mMyElm = e;
            mDeltaY = 0;
            mSim = s;
            mSim.PushUndo();
            setupValues();

            Text = mName;

            mPnlV = new Panel();
            {
                mPnlV.Left = 4;
                mPnlV.Top = 4;
                int ofsY = 0;
                /* label */
                mLabels = new Label() { Text = "---" };
                mLabels.AutoSize = true;
                mLabels.Left = 4;
                mLabels.Top = ofsY;
                mPnlV.Controls.Add(mLabels);
                ofsY += mLabels.Height;
                /* trbValue */
                mTrbValue = new TrackBar() {
                    Minimum = 0,
                    Maximum = mNValues - 1,
                    SmallChange = 1,
                    LargeChange = 1,
                    TickFrequency = mNValues / 24,
                    TickStyle = TickStyle.TopLeft,
                    Width = 300,
                    Height = 21,
                    Left = 4,
                    Top = ofsY
                };
                ofsY += mTrbValue.Height * 2 / 3;
                mTrbValue.ValueChanged += new EventHandler((sender, ev) => { setElmValue((TrackBar)sender); });
                mPnlV.Width = mTrbValue.Width + 8;
                mPnlV.Height = ofsY + 4;
                mPnlV.Controls.Add(mTrbValue);
                /* */
                Controls.Add(mPnlV);
            }

            doDeltaY(dy);
            Width = mPnlV.Width + 24;
            Height = mPnlV.Height + 48;
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
        }

        public void Show(int x, int y) {
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Show();
            Left = x - Width / 2;
            Top = y - Height / 2;
            Visible = true;
        }

        public void Close(bool keepChanges) {
            if (!keepChanges) {
                setElmValue(mCurrentIdx);
            } else {
                setElmValue();
            }
            Close();
        }

        void setupValues() {
            if (mMyElm is ResistorElm) {
                mMinPow = 0;
                mMaxPow = 6;
                mUnit = "Ω";
            }
            if (mMyElm is CapacitorElm) {
                mMinPow = -11;
                mMaxPow = -3;
                mUnit = "F";
            }
            if (mMyElm is InductorElm) {
                mMinPow = -6;
                mMaxPow = 0;
                mUnit = "H";
            }
            mValues = new double[2 + (mMaxPow - mMinPow) * 24];
            int ptr = 0;
            for (int i = mMinPow; i <= mMaxPow; i++) {
                for (int j = 0; j < ((i != mMaxPow) ? 24 : 1); j++, ptr++) {
                    mValues[ptr] = Math.Pow(10.0, i) * E24[j];
                }
            }
            mNValues = ptr;
            mValues[mNValues] = 1E99;
            mInfo = mMyElm.GetElementInfo(0);
            double currentvalue = mInfo.Value;
            for (int i = 0; i < mNValues + 1; i++) {
                if (Utils.ShortUnitText(currentvalue, "") == Utils.ShortUnitText(mValues[i], "")) { /* match to an existing value */
                    mValues[i] = currentvalue; /* Just in case it isn't 100% identical */
                    mCurrentIdx = i;
                    break;
                }
                if (currentvalue < mValues[i]) { /* overshot - need to insert value */
                    mCurrentIdx = i;
                    for (int j = mNValues - 1; j >= i; j--) {
                        mValues[j + 1] = mValues[j];
                    }
                    mValues[i] = currentvalue;
                    mNValues++;
                    break;
                }
            }
            mName = mInfo.Name;
            mLastIdx = mCurrentIdx;
            /*for (int i = 0; i < nvalues; i++) {
                Console.WriteLine("i=" + i + " values=" + values[i] + " current? " + (i == currentidx));
            }*/
        }

        void setupLabels() {
            int thissel = getSelIdx();
            mLabels.Text = Utils.ShortUnitText(mValues[thissel], mUnit);
            mTrbValue.Value = thissel;
        }

        void onMouseDown(MouseEventArgs e) {
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle) {
                Close(true);
            } else {
                Close(false);
            }
        }

        void doDeltaY(int dy) {
            mDeltaY += dy;
            if (mCurrentIdx + mDeltaY / 3 < 0) {
                mDeltaY = -3 * mCurrentIdx;
            }
            if (mCurrentIdx + mDeltaY / 3 >= mNValues) {
                mDeltaY = (mNValues - mCurrentIdx - 1) * 3;
            }
            setElmValue();
            setupLabels();
        }

        void setElmValue() {
            int idx = getSelIdx();
            setElmValue(idx);
        }

        void setElmValue(TrackBar tr) {
            mLastIdx = mCurrentIdx;
            mCurrentIdx = tr.Value;
            int thissel = getSelIdx();
            mInfo.Value = mValues[thissel];
            mMyElm.SetElementValue(0, mInfo);
            mSim.NeedAnalyze();
            mLabels.Text = Utils.ShortUnitText(mValues[thissel], mUnit);
        }

        void setElmValue(int i) {
            if (i != mLastIdx) {
                mTrbValue.Value = i;
                mLastIdx = i;
                mInfo.Value = mValues[i];
                mMyElm.SetElementValue(0, mInfo);
                mSim.NeedAnalyze();
            }
        }

        int getSelIdx() {
            var r = mCurrentIdx + mDeltaY / 3;
            if (r < 0) {
                r = 0;
            }
            if (r >= mNValues) {
                r = mNValues - 1;
            }
            return r;
        }
    }
}
