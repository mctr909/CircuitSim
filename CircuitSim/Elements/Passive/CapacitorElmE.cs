namespace Circuit.Elements.Passive {
    class CapacitorElmE : BaseElement {
        double mCompResistance;
        double mCurSourceValue;

        public double VoltDiff { get; private set; }

        public CapacitorElmE() {
            Capacitance = 1e-5;
        }

        public CapacitorElmE(double capacitance, double voltDiff) {
            Capacitance = capacitance;
            VoltDiff = voltDiff;
        }

        protected override void cirCalculateCurrent() {
            double voltdiff = CirVolts[0] - CirVolts[1];
            if (CirSim.Sim.DcAnalysisFlag) {
                mCirCurrent = voltdiff / 1e8;
                return;
            }
            /* we check compResistance because this might get called
             * before stamp(), which sets compResistance, causing
             * infinite current */
            if (0 < mCompResistance) {
                mCirCurrent = voltdiff / mCompResistance + mCurSourceValue;
            }
        }

        public override void CirShorted() {
            base.CirReset();
            VoltDiff = mCirCurrent = mCirCurCount = mCurSourceValue = 0;
        }

        public override void CirSetNodeVoltage(int n, double c) {
            base.CirSetNodeVoltage(n, c);
            VoltDiff = CirVolts[0] - CirVolts[1];
        }

        public override void CirStamp() {
            if (CirSim.Sim.DcAnalysisFlag) {
                /* when finding DC operating point, replace cap with a 100M resistor */
                mCir.StampResistor(CirNodes[0], CirNodes[1], 1e8);
                mCurSourceValue = 0;
                return;
            }

            mCompResistance = ControlPanel.TimeStep / (2 * Capacitance);

            mCir.StampResistor(CirNodes[0], CirNodes[1], mCompResistance);
            mCir.StampRightSide(CirNodes[0]);
            mCir.StampRightSide(CirNodes[1]);
        }

        public override void CirStartIteration() {
            mCurSourceValue = -VoltDiff / mCompResistance - mCirCurrent;
        }

        public override void CirDoStep() {
            if (CirSim.Sim.DcAnalysisFlag) {
                return;
            }
            mCir.StampCurrentSource(CirNodes[0], CirNodes[1], mCurSourceValue);
        }

        public override void CirReset() {
            base.CirReset();
            mCirCurrent = mCirCurCount = mCurSourceValue = 0;
            /* put small charge on caps when reset to start oscillators */
            VoltDiff = 1e-3;
        }
    }
}
