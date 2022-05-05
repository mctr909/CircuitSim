using System;

namespace Circuit.Elements.Gate {
    class InverterElmE : BaseElement {
        public double SlewRate; /* V/ns */
        public double HighVoltage;
        double mLastOutputVoltage;

        public InverterElmE() : base() {
            SlewRate = 0.5;
            /* copy defaults from last gate edited */
            HighVoltage = GateElmE.LastHighVoltage;
        }

        public InverterElmE(StringTokenizer st) : base() {
            try {
                SlewRate = st.nextTokenDouble();
                HighVoltage = st.nextTokenDouble();
            } catch {
                SlewRate = 0.5;
                HighVoltage = 5;
            }
        }

        public override double CirVoltageDiff { get { return CirVolts[0]; } }

        public override int CirVoltageSourceCount { get { return 1; } }

        public override bool CirHasGroundConnection(int n1) { return n1 == 1; }

        public override double CirGetCurrentIntoNode(int n) {
            if (n == 1) {
                return mCirCurrent;
            }
            return 0;
        }

        public override void CirStamp() {
            mCir.StampVoltageSource(0, CirNodes[1], mCirVoltSource);
        }

        public override void CirStartIteration() {
            mLastOutputVoltage = CirVolts[1];
        }

        public override void CirDoStep() {
            double v = CirVolts[0] > HighVoltage * .5 ? 0 : HighVoltage;
            double maxStep = SlewRate * ControlPanel.TimeStep * 1e9;
            v = Math.Max(Math.Min(mLastOutputVoltage + maxStep, v), mLastOutputVoltage - maxStep);
            mCir.UpdateVoltageSource(0, CirNodes[1], mCirVoltSource, v);
        }
    }
}
