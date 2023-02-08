namespace Circuit.Elements.Passive {
    class ElmInductor : BaseElement {
        double mCompResistance;
        double mCurSourceValue;

        public double Inductance = 0.001;

        public ElmInductor() : base() { }

        public ElmInductor(double inductance, double c) : base() {
            Inductance = inductance;
            mCurrent = c;
        }

        public override int PostCount { get { return 2; } }

        public override bool NonLinear { get { return false; } }

        public void Setup(double ic, double cr) {
            Inductance = ic;
            mCurrent = cr;
        }

        public override void Reset() {
            mCurrent = Volts[0] = Volts[1] = CurCount = mCurSourceValue = 0;
        }

        public override void AnaStamp() {
            mCompResistance = 2 * Inductance / ControlPanel.TimeStep;
            Circuit.StampResistor(Nodes[0], Nodes[1], mCompResistance);
            Circuit.StampRightSide(Nodes[0]);
            Circuit.StampRightSide(Nodes[1]);
        }

        public override void CirPrepareIteration() {
            mCurSourceValue = (Volts[0] - Volts[1]) / mCompResistance + mCurrent;
        }

        public override void CirDoIteration() {
            var r = Circuit.RowInfo[Nodes[0] - 1].MapRow;
            Circuit.RightSide[r] -= mCurSourceValue;
            r = Circuit.RowInfo[Nodes[1] - 1].MapRow;
            Circuit.RightSide[r] += mCurSourceValue;
        }

        public override void CirSetVoltage(int n, double c) {
            Volts[n] = c;
            mCurrent = (Volts[0] - Volts[1]) / mCompResistance + mCurSourceValue;
        }
    }
}
