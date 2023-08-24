using System;

namespace Circuit.Elements.Input {
    class ElmAM : BaseElement {
        public double CarrierFreq;
        public double SignalFreq;
        public double Depth;
        public double MaxVoltage;
        public double Phase;

        double mFreqTimeZero;

        public ElmAM() : base() {
            MaxVoltage = 5;
            CarrierFreq = 1000;
            SignalFreq = 50;
            Depth = 0.1;
            Phase = 0.0;
            Reset();
        }

        public ElmAM(StringTokenizer st) : base() {
            CarrierFreq = st.nextTokenDouble(1000);
            SignalFreq = st.nextTokenDouble(50);
            MaxVoltage = st.nextTokenDouble(5);
            Phase = st.nextTokenDouble();
            Depth = st.nextTokenDouble(0.1);
            Reset();
        }

        public override int PostCount { get { return 1; } }

        public override int AnaVoltageSourceCount { get { return 1; } }

        public override double GetVoltageDiff() { return Volts[0]; }

        public override void Reset() {
            mFreqTimeZero = 0;
        }

        public override bool AnaHasGroundConnection(int n1) { return true; }

        public override void AnaStamp() {
            Circuit.StampVoltageSource(0, Nodes[0], mVoltSource);
        }

        public override void CirDoIteration() {
            var vn = Circuit.Nodes.Count + mVoltSource;
            var row = Circuit.RowInfo[vn - 1].MapRow;
            var th = 2 * Math.PI * (Circuit.Time - mFreqTimeZero);
            Circuit.RightSide[row] += (Math.Sin(th * SignalFreq + Phase) * Depth + 2 - Depth) / 2 * Math.Sin(th * CarrierFreq) * MaxVoltage;
        }
    }
}
