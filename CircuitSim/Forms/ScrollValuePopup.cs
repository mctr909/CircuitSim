using System;
using System.Windows.Forms;

using Circuit.Elements;
using Circuit.Elements.Passive;

namespace Circuit {
    class ScrollValuePopup : Form {
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

        CirSimForm mSim;
        BaseUI mMyElm;
        ElementInfo mInfo;
        Panel mPnlV;
        Label mLabels;
        TrackBar mTrbValue;
        int mDeltaY;
        string mName;
        string mUnit;

        public ScrollValuePopup(int dy, BaseUI e, CirSimForm s) : base() {
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
            if (mMyElm is ResistorUI) {
                mMinPow = -2;
                mMaxPow = 6;
                mUnit = "Ω";
            }
            if (mMyElm is CapacitorUI) {
                mMinPow = -11;
                mMaxPow = -3;
                mUnit = "F";
            }
            if (mMyElm is InductorUI) {
                mMinPow = -6;
                mMaxPow = 0;
                mUnit = "H";
            }
            var valDiv = 12;
            var valArr = E12;
            mValues = new double[2 + (mMaxPow - mMinPow) * valDiv];
            int ptr = 0;
            for (int i = mMinPow; i <= mMaxPow; i++) {
                for (int j = 0; j < ((i != mMaxPow) ? valDiv : 1); j++, ptr++) {
                    mValues[ptr] = Math.Pow(10.0, i) * valArr[j];
                }
            }
            mNValues = ptr;
            mValues[mNValues] = 1E99;
            mInfo = mMyElm.GetElementInfo(0);
            double currentvalue = mInfo.Value;
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
            mName = mInfo.Name;
            mLastIdx = mCurrentIdx;
        }

        void setupLabels() {
            int thissel = getSelIdx();
            mLabels.Text = Utils.UnitText(mValues[thissel], mUnit);
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
            mLabels.Text = Utils.UnitText(mValues[thissel], mUnit);
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
