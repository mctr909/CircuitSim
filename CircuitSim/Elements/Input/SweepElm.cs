using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Input {
    class SweepElm : CircuitElm {
        const int FLAG_LOG = 1;
        const int FLAG_BIDIR = 2;

        const int SIZE = 28;

        double mMaxV;
        double mMaxF;
        double mMinF;
        double mSweepTime;
        double mFrequency;

        double mFadd;
        double mFmul;
        double mFreqTime;
        double mSavedTimeStep;
        double mVolt;
        int mFdir = 1;

        Point mTextPos;

        public SweepElm(Point pos) : base(pos) {
            mMinF = 20;
            mMaxF = 4000;
            mMaxV = 5;
            mSweepTime = .1;
            mFlags = FLAG_BIDIR;
            Reset();
        }

        public SweepElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            mMinF = st.nextTokenDouble();
            mMaxF = st.nextTokenDouble();
            mMaxV = st.nextTokenDouble();
            mSweepTime = st.nextTokenDouble();
            Reset();
        }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override double Power { get { return -VoltageDiff * mCurrent; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override int PostCount { get { return 1; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.SWEEP; } }

        public override bool HasGroundConnection(int n1) { return true; }

        protected override string dump() {
            return mMinF
                + " " + mMaxF
                + " " + mMaxV
                + " " + mSweepTime;
        }

        public override void StartIteration() {
            /* has timestep been changed? */
            if (ControlPanel.TimeStep != mSavedTimeStep) {
                setParams();
            }
            mVolt = Math.Sin(mFreqTime) * mMaxV;
            mFreqTime += mFrequency * 2 * Math.PI * ControlPanel.TimeStep;
            mFrequency = mFrequency * mFmul + mFadd;
            if (mFrequency >= mMaxF && mFdir == 1) {
                if ((mFlags & FLAG_BIDIR) != 0) {
                    mFadd = -mFadd;
                    mFmul = 1 / mFmul;
                    mFdir = -1;
                } else {
                    mFrequency = mMinF;
                }
            }
            if (mFrequency <= mMinF && mFdir == -1) {
                mFadd = -mFadd;
                mFmul = 1 / mFmul;
                mFdir = 1;
            }
        }

        public override void DoStep() {
            mCir.UpdateVoltageSource(0, Nodes[0], mVoltSource, mVolt);
        }

        public override void Stamp() {
            mCir.StampVoltageSource(0, Nodes[0], mVoltSource);
        }

        public override void SetPoints() {
            base.SetPoints();
            setLead1(1 - 0.5 * SIZE / mLen);
            interpPoint(ref mTextPos, 1.0 + 0.66 * SIZE / Utils.Distance(mPoint1, mPoint2), 24 * mDsign);
        }

        public override void Reset() {
            mFrequency = mMinF;
            mFreqTime = 0;
            mFdir = 1;
            setParams();
        }

        public override void Draw(CustomGraphics g) {
            setBbox(mPoint1, mPoint2, SIZE);

            drawVoltage(0, mPoint1, mLead1);

            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;

            int xc = mPoint2.X;
            int yc = mPoint2.Y;
            g.DrawCircle(mPoint2, SIZE / 2);

            adjustBbox(
                xc - SIZE, yc - SIZE,
                xc + SIZE, yc + SIZE
            );

            int wl = 7;
            int xl = 10;
            long tm = DateTime.Now.ToFileTimeUtc();
            tm %= 2000;
            if (tm > 1000) {
                tm = 2000 - tm;
            }
            double w = 1 + tm * 0.002;
            if (CirSim.Sim.IsRunning) {
                w = 1 + 3 * (mFrequency - mMinF) / (mMaxF - mMinF);
            }

            int x0 = 0;
            int y0 = 0;
            g.LineColor = CustomGraphics.GrayColor;
            for (int i = -xl; i <= xl; i++) {
                var yy = yc + (int)(0.95 * Math.Sin(i * Math.PI * w / xl) * wl);
                if (i == -xl) {
                    x0 = xc + i;
                    y0 = yy;
                } else {
                    g.DrawLine(x0, y0, xc + i, yy);
                    x0 = xc + i;
                    y0 = yy;
                }
            }

            if (ControlPanel.ChkShowValues.Checked) {
                string s = Utils.ShortUnitText(mFrequency, "Hz");
                drawValues(s, 20, -15);
            }

            drawPosts();
            mCurCount = updateDotCount(-mCurrent, mCurCount);
            if (CirSim.Sim.DragElm != this) {
                drawDots(mPoint1, mLead1, mCurCount);
            }
        }

        void setParams() {
            if (mFrequency < mMinF || mFrequency > mMaxF) {
                mFrequency = mMinF;
                mFreqTime = 0;
                mFdir = 1;
            }
            if ((mFlags & FLAG_LOG) == 0) {
                mFadd = mFdir * ControlPanel.TimeStep * (mMaxF - mMinF) / mSweepTime;
                mFmul = 1;
            } else {
                mFadd = 0;
                mFmul = Math.Pow(mMaxF / mMinF, mFdir * ControlPanel.TimeStep / mSweepTime);
            }
            mSavedTimeStep = ControlPanel.TimeStep;
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "sweep " + (((mFlags & FLAG_LOG) == 0) ? "(linear)" : "(log)");
            arr[1] = "I = " + Utils.CurrentDText(mCurrent);
            arr[2] = "V = " + Utils.VoltageText(Volts[0]);
            arr[3] = "f = " + Utils.UnitText(mFrequency, "Hz");
            arr[4] = "range = " + Utils.UnitText(mMinF, "Hz") + " .. " + Utils.UnitText(mMaxF, "Hz");
            arr[5] = "time = " + Utils.UnitText(mSweepTime, "s");
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("振幅(V)", mMaxV, 0, 0);
            }
            if (n == 1) {
                return new ElementInfo("最小周波数(Hz)", mMinF, 0, 0);
            }
            if (n == 2) {
                return new ElementInfo("最大周波数(Hz)", mMaxF, 0, 0);
            }
            if (n == 3) {
                return new ElementInfo("スウィープ時間(sec)", mSweepTime, 0, 0);
            }
            if (n == 4) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = "周波数対数変化",
                    Checked = (mFlags & FLAG_LOG) != 0
                };
                return ei;
            }
            if (n == 5) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    AutoSize = true,
                    Text = "双方向周波数遷移",
                    Checked = (mFlags & FLAG_BIDIR) != 0
                };
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            double maxfreq = 1 / (8 * ControlPanel.TimeStep);
            if (n == 0) {
                mMaxV = ei.Value;
            }
            if (n == 1) {
                mMinF = ei.Value;
                if (mMinF > maxfreq) {
                    mMinF = maxfreq;
                }
            }
            if (n == 2) {
                mMaxF = ei.Value;
                if (mMaxF > maxfreq) {
                    mMaxF = maxfreq;
                }
            }
            if (n == 3) {
                mSweepTime = ei.Value;
            }
            if (n == 4) {
                mFlags &= ~FLAG_LOG;
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_LOG;
                }
            }
            if (n == 5) {
                mFlags &= ~FLAG_BIDIR;
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_BIDIR;
                }
            }
            setParams();
        }
    }
}
