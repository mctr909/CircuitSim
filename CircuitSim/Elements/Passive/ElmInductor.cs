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
            mCompResistance = 2 * Inductance / ControlPanel.TimeStep;
            Circuit.StampResistor(Nodes[0], Nodes[1], mCompResistance);
            Circuit.StampRightSide(Nodes[0]);
            Circuit.StampRightSide(Nodes[1]);
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
