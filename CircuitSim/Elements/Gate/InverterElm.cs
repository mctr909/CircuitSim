using System;

namespace Circuit.Elements.Gate {
    class InverterElm : BaseElement {
        public double SlewRate; /* V/ns */
        public double HighVoltage;
        double mLastOutputVoltage;

        public InverterElm() : base() {
            SlewRate = 0.5;
            /* copy defaults from last gate edited */
            HighVoltage = GateElm.LastHighVoltage;
        }

        public InverterElm(StringTokenizer st) : base() {
            try {
                SlewRate = st.nextTokenDouble();
                HighVoltage = st.nextTokenDouble();
            } catch {
                SlewRate = 0.5;
                HighVoltage = 5;
            }
        }

        public override int PostCount { get { return 2; } }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override bool HasGroundConnection(int n1) { return n1 == 1; }

        public override double GetCurrentIntoNode(int n) {
            if (n == 1) {
                return mCurrent;
            }
            return 0;
        }

        public override void Stamp() {
            mCir.StampVoltageSource(0, Nodes[1], mVoltSource);
        }

        public override void StartIteration() {
            mLastOutputVoltage = Volts[1];
        }

        public override void DoStep() {
            double v = Volts[0] > HighVoltage * .5 ? 0 : HighVoltage;
            double maxStep = SlewRate * ControlPanel.TimeStep * 1e9;
            v = Math.Max(Math.Min(mLastOutputVoltage + maxStep, v), mLastOutputVoltage - maxStep);
            mCir.UpdateVoltageSource(0, Nodes[1], mVoltSource, v);
        }
    }
}
