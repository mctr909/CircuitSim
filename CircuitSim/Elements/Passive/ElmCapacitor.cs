namespace Circuit.Elements.Passive {
    class ElmCapacitor : BaseElement {
        double mCompResistance;
        double mCurSourceValue;

        public double Capacitance = 1e-5;

        public double VoltDiff = 0.0;

        public override int PostCount { get { return 2; } }

        public override void Reset() {
            base.Reset();
            mCurrent = CurCount = mCurSourceValue = 0;
            /* put small charge on caps when reset to start oscillators */
            VoltDiff = 1e-3;
        }

        public override void AnaShorted() {
            base.Reset();
            VoltDiff = mCurrent = CurCount = mCurSourceValue = 0;
        }

        public override void AnaStamp() {
            mCompResistance = ControlPanel.TimeStep / (2 * Capacitance);
            Circuit.StampResistor(Nodes[0], Nodes[1], mCompResistance);
            Circuit.StampRightSide(Nodes[0]);
            Circuit.StampRightSide(Nodes[1]);
        }

        public override void CirDoIteration() {
            Circuit.StampCurrentSource(Nodes[0], Nodes[1], mCurSourceValue);
        }

        public override void CirPrepareIteration() {
            mCurSourceValue = -VoltDiff / mCompResistance - mCurrent;
        }

        public override void CirSetVoltage(int n, double c) {
            Volts[n] = c;
            VoltDiff = Volts[0] - Volts[1];
            mCurrent = VoltDiff / mCompResistance + mCurSourceValue;
        }
    }
}
