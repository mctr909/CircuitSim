namespace Circuit.Elements.Passive {
    class InductorElm : BaseElement {
        double mCompResistance;
        double mCurSourceValue;

        public double Inductance { get; set; }

        public InductorElm() : base() {
            Inductance = 0.001;
        }

        public InductorElm(StringTokenizer st) : base() {
            Inductance = st.nextTokenDouble();
            mCurrent = st.nextTokenDouble();
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

        public override void CirDoStep() {
            Circuit.StampCurrentSource(Nodes[0], Nodes[1], mCurSourceValue);
        }

        public override void CirStartIteration() {
            double voltdiff = Volts[0] - Volts[1];
            mCurSourceValue = voltdiff / mCompResistance + mCurrent;
        }

        public override void CirSetNodeVoltage(int n, double c) {
            Volts[n] = c;
            var voltdiff = Volts[0] - Volts[1];
            if (mCompResistance > 0) {
                mCurrent = voltdiff / mCompResistance + mCurSourceValue;
            }
        }

        public void Setup(double ic, double cr) {
            Inductance = ic;
            mCurrent = cr;
        }
    }
}
