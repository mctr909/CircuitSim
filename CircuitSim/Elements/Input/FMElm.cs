using System;
using System.Drawing;

namespace Circuit.Elements.Input {
    class FMElm : CircuitElm {
        const int FLAG_COS = 2;
        const int CR = 28;

        double mCarrierFreq;
        double mSignalfreq;
        double mMaxVoltage;
        double mFreqTimeZero;
        double mDeviation;
        double mLastTime = 0;
        double mFuncx = 0;

        public FMElm(Point pos) : base(pos) {
            mDeviation = 200;
            mMaxVoltage = 5;
            mCarrierFreq = 800;
            mSignalfreq = 40;
            Reset();
        }

        public FMElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            mCarrierFreq = st.nextTokenDouble();
            mSignalfreq = st.nextTokenDouble();
            mMaxVoltage = st.nextTokenDouble();
            mDeviation = st.nextTokenDouble();
            if ((mFlags & FLAG_COS) != 0) {
                mFlags &= ~FLAG_COS;
            }
            Reset();
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.FM; } }

        protected override string dump() {
            return mCarrierFreq + " " + mSignalfreq + " " + mMaxVoltage + " " + mDeviation;
        }

        public override void Reset() {
            mFreqTimeZero = 0;
            mCurCount = 0;
        }

        public override int PostCount { get { return 1; } }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override double Power { get { return -VoltageDiff * mCurrent; } }

        public override bool HasGroundConnection(int n1) { return true; }

        public override void Stamp() {
            mCir.StampVoltageSource(0, Nodes[0], mVoltSource);
        }

        public override void DoStep() {
            mCir.UpdateVoltageSource(0, Nodes[0], mVoltSource, getVoltage());
        }

        double getVoltage() {
            double deltaT = CirSim.Sim.Time - mLastTime;
            mLastTime = CirSim.Sim.Time;
            double signalamplitude = Math.Sin(2 * Math.PI * (CirSim.Sim.Time - mFreqTimeZero) * mSignalfreq);
            mFuncx += deltaT * (mCarrierFreq + (signalamplitude * mDeviation));
            double w = 2 * Math.PI * mFuncx;
            return Math.Sin(w) * mMaxVoltage;
        }

        public override void SetPoints() {
            base.SetPoints();
            setLead1(1 - 0.5 * CR / mLen);
        }

        public override void Draw() {
            setBbox(mPoint1, mPoint2, CR);
            drawVoltage(0, mPoint1, mLead1);

            CustomGraphics.TextColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.WhiteColor;
            double v = getVoltage();
            string s = "FM";
            drawCenteredText(s, P2.X, P2.Y, true);
            drawWaveform(mPoint2);
            drawPosts();
            mCurCount = updateDotCount(-mCurrent, mCurCount);
            if (CirSim.Sim.DragElm != this) {
                drawDots(mPoint1, mLead1, mCurCount);
            }
        }

        void drawWaveform(Point center) {
            Context.ThickLineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            int xc = center.X;
            int yc = center.Y;
            Context.DrawThickCircle(center, CR);
            adjustBbox(xc - CR, yc - CR, xc + CR, yc + CR);
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "FM Source";
            arr[1] = "I = " + Utils.CurrentText(Current);
            arr[2] = "V = " + Utils.VoltageText(VoltageDiff);
            arr[3] = "cf = " + Utils.UnitText(mCarrierFreq, "Hz");
            arr[4] = "sf = " + Utils.UnitText(mSignalfreq, "Hz");
            arr[5] = "dev =" + Utils.UnitText(mDeviation, "Hz");
            arr[6] = "Vmax = " + Utils.VoltageText(mMaxVoltage);
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("振幅(V)", mMaxVoltage, -20, 20);
            }
            if (n == 1) {
                return new ElementInfo("搬送波周波数(Hz)", mCarrierFreq, 4, 500);
            }
            if (n == 2) {
                return new ElementInfo("信号周波数(Hz)", mSignalfreq, 4, 500);
            }
            if (n == 3) {
                return new ElementInfo("周波数偏移(Hz)", mDeviation, 4, 500);
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0) {
                mMaxVoltage = ei.Value;
            }
            if (n == 1) {
                mCarrierFreq = ei.Value;
            }
            if (n == 2) {
                mSignalfreq = ei.Value;
            }
            if (n == 3) {
                mDeviation = ei.Value;
            }
        }
    }
}
