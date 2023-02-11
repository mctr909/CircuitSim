namespace Circuit.Elements.Passive {
    class ElmCapacitor : BaseElement {
        double mCompResistance;
        double mCurSourceValue;

        public double Capacitance = 1e-5;

        public double VoltDiff = 0.0;

        public override int PostCount { get { return 2; } }

        public override void Reset() {
            base.Reset();
            Current = mCurSourceValue = 0;
            /* put small charge on caps when reset to start oscillators */
            VoltDiff = 1e-3;
        }

        public override void AnaShorted() {
            base.Reset();
            VoltDiff = Current = mCurSourceValue = 0;
        }

        public override void AnaStamp() {
            var g = 2 * Capacitance / ControlPanel.TimeStep;
            var n0 = Nodes[0] - 1;
            var n1 = Nodes[1] - 1;
            mCompResistance = 1.0 / g;
            Circuit.Matrix[n0, n0] += g;
            Circuit.Matrix[n1, n1] += g;
            Circuit.Matrix[n0, n1] -= g;
            Circuit.Matrix[n1, n0] -= g;
            Circuit.RowInfo[n0].RightChanges = true;
            Circuit.RowInfo[n1].RightChanges = true;
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
