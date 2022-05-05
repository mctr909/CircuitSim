namespace Circuit.Elements.Output {
    class LogicOutputElmE : BaseElement {
        public double mThreshold;
        public string mValue;

        public bool needsPullDown;

        public LogicOutputElmE() : base() {
            mThreshold = 2.5;
        }

        public LogicOutputElmE(StringTokenizer st) : base() {
            try {
                mThreshold = st.nextTokenDouble();
            } catch {
                mThreshold = 2.5;
            }
        }

        public override double CirVoltageDiff { get { return CirVolts[0]; } }

        public override int CirPostCount { get { return 1; } }

        public override void CirStamp() {
            if (needsPullDown) {
                mCir.StampResistor(CirNodes[0], 0, 1e6);
            }
        }
    }
}
