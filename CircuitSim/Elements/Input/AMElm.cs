using System;
using System.Drawing;

namespace Circuit.Elements.Input {
    class AMElm : CircuitElm {
        const int FLAG_COS = 2;
        const int SIZE = 28;

        double mCarrierFreq;
        double mSignalFreq;
        double mDepth;
        double mMaxVoltage;
        double mFreqTimeZero;
        double mPhase;

        public AMElm(Point pos) : base(pos) {
            mMaxVoltage = 5;
            mCarrierFreq = 1000;
            mSignalFreq = 40;
            mDepth = 0.1;
            mPhase = 0.0;
            Reset();
        }

        public AMElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            try {
                mCarrierFreq = st.nextTokenDouble();
                mSignalFreq = st.nextTokenDouble();
                mMaxVoltage = st.nextTokenDouble();
                mPhase = st.nextTokenDouble();
                mDepth = st.nextTokenDouble();
            } catch { }
            if ((mFlags & FLAG_COS) != 0) {
                mFlags &= ~FLAG_COS;
            }
            Reset();
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.AM; } }

        protected override string dump() {
            return mCarrierFreq
                + " " + mSignalFreq
                + " " + mMaxVoltage
                + " " + mPhase
                + " " + mDepth;
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

        public override void SetPoints() {
            base.SetPoints();
            setLead1(1 - 0.5 * SIZE / mLen);
        }

        public override void Draw(CustomGraphics g) {
            setBbox(mPoint1, mPoint2, SIZE);
            drawLead(mPoint1, mLead1);

            double v = getVoltage();
            string s = "AM";
            drawCenteredText(s, P2, true);
            drawWaveform(g, mPoint2);
            drawPosts();
            mCurCount = updateDotCount(-mCurrent, mCurCount);
            if (CirSim.Sim.DragElm != this) {
                drawDots(mPoint1, mLead1, mCurCount);
            }
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "AM Source";
            arr[1] = "I = " + Utils.CurrentText(Current);
            arr[2] = "V = " + Utils.VoltageText(VoltageDiff);
            arr[3] = "cf = " + Utils.UnitText(mCarrierFreq, "Hz");
            arr[4] = "sf = " + Utils.UnitText(mSignalFreq, "Hz");
            arr[5] = "Vmax = " + Utils.VoltageText(mMaxVoltage);
        }

        void drawWaveform(CustomGraphics g, Point center) {
            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            int xc = center.X;
            int yc = center.Y;
            g.DrawCircle(center, SIZE / 2);
            adjustBbox(xc - SIZE, yc - SIZE, xc + SIZE, yc + SIZE);
        }

        double getVoltage() {
            double w = 2 * Math.PI * (CirSim.Sim.Time - mFreqTimeZero);
            return (Math.Sin(w * mSignalFreq + mPhase) * mDepth + 2 - mDepth) / 2 * Math.Sin(w * mCarrierFreq) * mMaxVoltage;
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("振幅(V)", mMaxVoltage, -20, 20);
            }
            if (n == 1) {
                return new ElementInfo("搬送波周波数(Hz)", mCarrierFreq, 4, 500);
            }
            if (n == 2) {
                return new ElementInfo("信号周波数(Hz)", mSignalFreq, 4, 500);
            }
            if (n == 3) {
                return new ElementInfo("変調度(%)", (int)(mDepth * 100), 0, 100);
            }
            if (n == 4) {
                return new ElementInfo("位相(degrees)", double.Parse((mPhase * 180 / Math.PI).ToString("0.00")), -180, 180).SetDimensionless();
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
                mSignalFreq = ei.Value;
            }
            if (n == 3) {
                mDepth = ei.Value * 0.01;
            }
            if (n == 4) {
                mPhase = ei.Value * Math.PI / 180;
            }
        }
    }
}
