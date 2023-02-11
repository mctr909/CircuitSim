namespace Circuit.Elements.Passive {
    class ElmInductor : BaseElement {
        double mCompResistance;
        double mCurSourceValue;

        public double Inductance = 0.001;

        public ElmInductor() : base() { }

        public ElmInductor(double inductance, double c) : base() {
            Inductance = inductance;
            Current = c;
        }

        public override int PostCount { get { return 2; } }

        public void Setup(double ic, double cr) {
            Inductance = ic;
            Current = cr;
        }

        public override void Reset() {
            Current = Volts[0] = Volts[1] = mCurSourceValue = 0;
        }

        public override void AnaStamp() {
            var g = ControlPanel.TimeStep / (2 * Inductance);
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
            mCurSourceValue = (Volts[0] - Volts[1]) / mCompResistance + Current;
        }

        public override void CirDoIteration() {
            var r = Circuit.RowInfo[Nodes[0] - 1].MapRow;
            Circuit.RightSide[r] -= mCurSourceValue;
            r = Circuit.RowInfo[Nodes[1] - 1].MapRow;
            Circuit.RightSide[r] += mCurSourceValue;
        }

        public override void CirSetVoltage(int n, double c) {
            Volts[n] = c;
            Current = (Volts[0] - Volts[1]) / mCompResistance + mCurSourceValue;
        }
    }
}
