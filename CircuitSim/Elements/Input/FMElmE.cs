using System;

namespace Circuit.Elements.Input {
    class FMElmE : BaseElement {
        public double CarrierFreq;
        public double Signalfreq;
        public double MaxVoltage;
        public double Deviation;

        double mFreqTimeZero;
        double mLastTime = 0;
        double mFuncx = 0;

        public FMElmE() : base() {
            Deviation = 200;
            MaxVoltage = 5;
            CarrierFreq = 800;
            Signalfreq = 40;
            CirReset();
        }

        public FMElmE(StringTokenizer st) : base() {
            CarrierFreq = st.nextTokenDouble();
            Signalfreq = st.nextTokenDouble();
            MaxVoltage = st.nextTokenDouble();
            Deviation = st.nextTokenDouble();
            CirReset();
        }

        public override void CirReset() {
            mFreqTimeZero = 0;
            mCirCurCount = 0;
        }

        public override int CirPostCount { get { return 1; } }

        public override double CirVoltageDiff { get { return CirVolts[0]; } }

        public override int CirVoltageSourceCount { get { return 1; } }

        public override double CirPower { get { return -CirVoltageDiff * mCirCurrent; } }

        public override bool CirHasGroundConnection(int n1) { return true; }

        public override void CirStamp() {
            mCir.StampVoltageSource(0, CirNodes[0], mCirVoltSource);
        }

        public override void CirDoStep() {
            mCir.UpdateVoltageSource(0, CirNodes[0], mCirVoltSource, getVoltage());
        }

        double getVoltage() {
            double deltaT = CirSim.Sim.Time - mLastTime;
            mLastTime = CirSim.Sim.Time;
            double signalamplitude = Math.Sin(2 * Math.PI * (CirSim.Sim.Time - mFreqTimeZero) * Signalfreq);
            mFuncx += deltaT * (CarrierFreq + (signalamplitude * Deviation));
            double w = 2 * Math.PI * mFuncx;
            return Math.Sin(w) * MaxVoltage;
        }
    }
}
