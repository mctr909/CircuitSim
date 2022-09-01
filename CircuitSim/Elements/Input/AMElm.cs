using System;

namespace Circuit.Elements.Input {
    class AMElm : BaseElement {
        public double CarrierFreq;
        public double SignalFreq;
        public double Depth;
        public double MaxVoltage;
        public double Phase;

        double mFreqTimeZero;

        public AMElm() : base() {
            MaxVoltage = 5;
            CarrierFreq = 1000;
            SignalFreq = 40;
            Depth = 0.1;
            Phase = 0.0;
            Reset();
        }

        public AMElm(StringTokenizer st) : base() {
            try {
                CarrierFreq = st.nextTokenDouble();
                SignalFreq = st.nextTokenDouble();
                MaxVoltage = st.nextTokenDouble();
                Phase = st.nextTokenDouble();
                Depth = st.nextTokenDouble();
            } catch { }
            Reset();
        }

        public override int PostCount { get { return 1; } }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override double Power { get { return -VoltageDiff * mCurrent; } }

        public override void Reset() {
            mFreqTimeZero = 0;
            CurCount = 0;
        }

        public override bool AnaHasGroundConnection(int n1) { return true; }

        public override void AnaStamp() {
            Circuit.StampVoltageSource(0, Nodes[0], mVoltSource);
        }

        public override void CirDoStep() {
            Circuit.UpdateVoltageSource(0, Nodes[0], mVoltSource, getVoltage());
        }

        double getVoltage() {
            double w = 2 * Math.PI * (CirSimForm.Time - mFreqTimeZero);
            return (Math.Sin(w * SignalFreq + Phase) * Depth + 2 - Depth) / 2 * Math.Sin(w * CarrierFreq) * MaxVoltage;
        }
    }
}
