using System;
using System.Windows.Forms;

using Circuit.UI;
using Circuit.UI.Passive;

namespace Circuit {
    class ScrollValuePopup : Form {
        static readonly double[] E6 = {
            1.0, 1.5,
            2.2, 3.3,
            4.7, 6.8,
        };
        static readonly double[] E12 = {
            1.0, 1.2, 1.5, 1.8,
            2.2, 2.7, 3.3, 3.9,
            4.7, 5.6, 6.8, 8.2
        };

        double[] mValues;
        int mMinPow = 0;
        int mMaxPow = 1;
        int mNValues;
        int mCurrentIdx;
        int mLastIdx;

        Panel mPnlV;
        Label mLabels;
        TrackBar mTrbValue;
        string mName;
        string mUnit;

        BaseUI mEditElm;
        ElementInfo mElmInfo;

        public ScrollValuePopup(BaseUI e) : base() {
            mEditElm = e;
            CirSimForm.PushUndo();
            setupValues();

            Text = mName;

            mPnlV = new Panel();
            {
                mPnlV.Left = 4;
                mPnlV.Top = 4;
                int ofsY = 0;

                /* label */
                mLabels = new Label() {
                    Text = "---",
                    AutoSize = true,
                    Left = 4,
                    Top = ofsY
                };
                mPnlV.Controls.Add(mLabels);
                ofsY += mLabels.Height;

                /* trbValue */
                mTrbValue = new TrackBar() {
                    Minimum = 0,
                    Maximum = mNValues - 1,
                    SmallChange = 1,
                    LargeChange = 1,
                    TickFrequency = mNValues / 12,
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

            setElmValue();
            setupLabels();
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
            if (mEditElm is Resistor) {
                mMinPow = -2;
                mMaxPow = 7;
                mUnit = CirSimForm.OHM_TEXT;
            }
            if (mEditElm is Pot) {
                mMinPow = 1;
                mMaxPow = 7;
                mUnit = CirSimForm.OHM_TEXT;
            }
            if (mEditElm is Capacitor) {
                mMinPow = -11;
                mMaxPow = -3;
                mUnit = "F";
            }
            if (mEditElm is Inductor) {
                mMinPow = -6;
                mMaxPow = 0;
                mUnit = "H";
            }
            var valDiv = 6;
            var valArr = E6;
            mValues = new double[2 + (mMaxPow - mMinPow) * valDiv];
            int ptr = 0;
            for (int i = mMinPow; i <= mMaxPow; i++) {
                for (int j = 0; j < ((i != mMaxPow) ? valDiv : 1); j++, ptr++) {
                    mValues[ptr] = Math.Pow(10.0, i) * valArr[j];
                }
            }
            mNValues = ptr;
            mValues[mNValues] = 1E99;
            mElmInfo = mEditElm.GetElementInfo(0, 0);
            double currentvalue = mElmInfo.Value;
            for (int i = 0; i < mNValues + 1; i++) {
                if (Utils.UnitText(currentvalue, "") == Utils.UnitText(mValues[i], "")) { /* match to an existing value */
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
            mName = mElmInfo.Name;
            mLastIdx = mCurrentIdx;
        }

        void setupLabels() {
            int thissel = getSelIdx();
            if ("F" == mUnit) {
                mLabels.Text = Utils.CapacitanceText(mValues[thissel], mUnit);
            } else {
                mLabels.Text = Utils.UnitText(mValues[thissel], mUnit);
            }
            mTrbValue.Value = thissel;
        }

        void setElmValue() {
            int idx = getSelIdx();
            setElmValue(idx);
        }

        void setElmValue(TrackBar tr) {
            mLastIdx = mCurrentIdx;
            mCurrentIdx = tr.Value;
            int thissel = getSelIdx();
            mElmInfo.Value = mValues[thissel];
            mEditElm.SetElementValue(0, 0, mElmInfo);
            CirSimForm.NeedAnalyze();
            if ("F" == mUnit) {
                mLabels.Text = Utils.CapacitanceText(mValues[thissel], mUnit);
            } else {
                mLabels.Text = Utils.UnitText(mValues[thissel], mUnit);
            }
        }

        void setElmValue(int i) {
            if (i != mLastIdx) {
                mTrbValue.Value = i;
                mLastIdx = i;
                mElmInfo.Value = mValues[i];
                mEditElm.SetElementValue(0, 0, mElmInfo);
                CirSimForm.NeedAnalyze();
            }
        }

        int getSelIdx() {
            var r = mCurrentIdx;
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
