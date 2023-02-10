namespace Circuit.Elements.Passive {
    class ElmCapacitor : BaseElement {
        double mCompResistance;
        double mCurSourceValue;

        public double Capacitance = 1e-5;

        public double VoltDiff = 0.0;

        public override int PostCount { get { return 2; } }

        public override void Reset() {
            base.Reset();
            Current = CurCount = mCurSourceValue = 0;
            /* put small charge on caps when reset to start oscillators */
            VoltDiff = 1e-3;
        }

        public override void AnaShorted() {
            base.Reset();
            VoltDiff = Current = CurCount = mCurSourceValue = 0;
        }

        public override void AnaStamp() {
            mCompResistance = ControlPanel.TimeStep / (2 * Capacitance);
            Circuit.StampResistor(Nodes[0], Nodes[1], mCompResistance);
            Circuit.StampRightSide(Nodes[0]);
            Circuit.StampRightSide(Nodes[1]);
        }

        public override void CirPrepareIteration() {
            mCurSourceValue = -VoltDiff / mCompResistance - Current;
        }

        public override void CirDoIteration() {
            var r = Circuit.RowInfo[Nodes[0] - 1].MapRow;
            Circuit.RightSide[r] -= mCurSourceValue;
            r = Circuit.RowInfo[Nodes[1] - 1].MapRow;
            Circuit.RightSide[r] += mCurSourceValue;
        }

        public override void CirSetVoltage(int n, double c) {
            Volts[n] = c;
            VoltDiff = Volts[0] - Volts[1];
            Current = VoltDiff / mCompResistance + mCurSourceValue;
        }
    }
}
