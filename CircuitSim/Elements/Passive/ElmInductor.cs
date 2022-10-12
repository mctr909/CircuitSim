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

        public override void Reset() {
            mCurrent = Volts[0] = Volts[1] = CurCount = mCurSourceValue = 0;
        }

        public override void AnaStamp() {
            mCompResistance = 2 * Inductance / ControlPanel.TimeStep;
            Circuit.StampResistor(Nodes[0], Nodes[1], mCompResistance);
            Circuit.StampRightSide(Nodes[0]);
            Circuit.StampRightSide(Nodes[1]);
        }

        public override void CirDoIteration() {
            Circuit.StampCurrentSource(Nodes[0], Nodes[1], mCurSourceValue);
        }

        public override void CirPrepareIteration() {
            double voltdiff = Volts[0] - Volts[1];
            mCurSourceValue = voltdiff / mCompResistance + mCurrent;
        }

        public override void CirSetVoltage(int n, double c) {
            Volts[n] = c;
            var voltdiff = Volts[0] - Volts[1];
            mCurrent = voltdiff / mCompResistance + mCurSourceValue;
        }

        public void Setup(double ic, double cr) {
            Inductance = ic;
            mCurrent = cr;
        }
    }
}
