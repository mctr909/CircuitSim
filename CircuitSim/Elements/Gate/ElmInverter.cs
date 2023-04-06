using System;

namespace Circuit.Elements.Gate {
    class ElmInverter : BaseElement {
        public double SlewRate; /* V/ns */
        public double HighVoltage;
        double mLastOutputVoltage;

        public ElmInverter() : base() {
            SlewRate = 0.5;
            /* copy defaults from last gate edited */
            HighVoltage = ElmGate.LastHighVoltage;
        }

        public ElmInverter(StringTokenizer st) : base() {
            try {
                SlewRate = st.nextTokenDouble();
                HighVoltage = st.nextTokenDouble();
            } catch {
                SlewRate = 0.5;
                HighVoltage = 5;
            }
        }

        public override int PostCount { get { return 2; } }

        public override int AnaVoltageSourceCount { get { return 1; } }

        public override double GetVoltageDiff() { return Volts[0]; }

        public override double CirGetCurrentIntoNode(int n) {
            if (n == 1) {
                return Current;
            }
            return 0;
        }

        /* there is no current path through the inverter input,
         * but there is an indirect path through the output to ground. */
        public override bool AnaGetConnection(int n1, int n2) { return false; }

        public override bool AnaHasGroundConnection(int n1) { return n1 == 1; }

        public override void AnaStamp() {
            Circuit.StampVoltageSource(0, Nodes[1], mVoltSource);
        }

        public override void CirPrepareIteration() {
            mLastOutputVoltage = Volts[1];
        }

        public override void CirDoIteration() {
            double v = Volts[0] > HighVoltage * .5 ? 0 : HighVoltage;
            double maxStep = SlewRate * ControlPanel.TimeStep * 1e9;
            v = Math.Max(Math.Min(mLastOutputVoltage + maxStep, v), mLastOutputVoltage - maxStep);
            Circuit.UpdateVoltageSource(mVoltSource, v);
        }
    }
}
