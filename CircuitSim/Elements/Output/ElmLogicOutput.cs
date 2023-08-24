namespace Circuit.Elements.Output {
    class ElmLogicOutput : BaseElement {
        public double mThreshold;
        public string mValue;

        public bool needsPullDown;

        public ElmLogicOutput() : base() {
            mThreshold = 2.5;
        }

        public ElmLogicOutput(StringTokenizer st) : base() {
            mThreshold = st.nextTokenDouble(2.5);
        }

        public override int PostCount { get { return 1; } }

        public override double GetVoltageDiff() { return Volts[0]; }

        public override void AnaStamp() {
            if (needsPullDown) {
                Circuit.StampResistor(Nodes[0], 0, 1e6);
            }
        }
    }
}
