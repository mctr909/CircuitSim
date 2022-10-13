using System;

namespace Circuit.Elements.Input {
    class ElmFM : BaseElement {
        public double CarrierFreq;
        public double Signalfreq;
        public double MaxVoltage;
        public double Deviation;

        double mFreqTimeZero;
        double mLastTime = 0;
        double mCounter = 0;

        public ElmFM() : base() {
            Deviation = 200;
            MaxVoltage = 5;
            CarrierFreq = 800;
            Signalfreq = 40;
            Reset();
        }

        public ElmFM(StringTokenizer st) : base() {
            CarrierFreq = st.nextTokenDouble();
            Signalfreq = st.nextTokenDouble();
            MaxVoltage = st.nextTokenDouble();
            Deviation = st.nextTokenDouble();
            Reset();
        }

        public override void Reset() {
            mFreqTimeZero = 0;
            CurCount = 0;
        }

        public override int PostCount { get { return 1; } }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override double Power { get { return -VoltageDiff * mCurrent; } }

        public override bool AnaHasGroundConnection(int n1) { return true; }

        public override void AnaStamp() {
            Circuit.StampVoltageSource(0, Nodes[0], mVoltSource);
        }

        public override void CirDoIteration() {
            var deltaT = CirSimForm.Time - mLastTime;
            var signalAmplitude = Math.Sin(2 * Math.PI * (CirSimForm.Time - mFreqTimeZero) * Signalfreq);
            mCounter += (CarrierFreq + (signalAmplitude * Deviation)) * deltaT;
            var vn = Circuit.NodeList.Count + mVoltSource;
            var row = Circuit.mRowInfo[vn - 1].MapRow;
            Circuit.mRightSide[row] += Math.Sin(2 * Math.PI * mCounter) * MaxVoltage;
            mLastTime = CirSimForm.Time;
        }
    }
}
