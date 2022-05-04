using System;

namespace Circuit.Elements.Input {
    class AMElmE : BaseElement {
        public double CarrierFreq;
        public double SignalFreq;
        public double Depth;
        public double MaxVoltage;
        public double Phase;

        double mFreqTimeZero;

        public AMElmE() : base() {
            MaxVoltage = 5;
            CarrierFreq = 1000;
            SignalFreq = 40;
            Depth = 0.1;
            Phase = 0.0;
            CirReset();
        }

        public AMElmE(StringTokenizer st) : base() {
            try {
                CarrierFreq = st.nextTokenDouble();
                SignalFreq = st.nextTokenDouble();
                MaxVoltage = st.nextTokenDouble();
                Phase = st.nextTokenDouble();
                Depth = st.nextTokenDouble();
            } catch { }
            CirReset();
        }

        public override int CirPostCount { get { return 1; } }

        public override double CirVoltageDiff { get { return CirVolts[0]; } }

        public override int CirVoltageSourceCount { get { return 1; } }

        public override double CirPower { get { return -CirVoltageDiff * mCirCurrent; } }

        public override void CirReset() {
            mFreqTimeZero = 0;
            mCirCurCount = 0;
        }

        public override bool CirHasGroundConnection(int n1) { return true; }

        public override void CirStamp() {
            mCir.StampVoltageSource(0, CirNodes[0], mCirVoltSource);
        }

        public override void CirDoStep() {
            mCir.UpdateVoltageSource(0, CirNodes[0], mCirVoltSource, getVoltage());
        }

        double getVoltage() {
            double w = 2 * Math.PI * (CirSim.Sim.Time - mFreqTimeZero);
            return (Math.Sin(w * SignalFreq + Phase) * Depth + 2 - Depth) / 2 * Math.Sin(w * CarrierFreq) * MaxVoltage;
        }
    }
}
