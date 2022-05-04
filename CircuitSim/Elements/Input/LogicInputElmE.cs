namespace Circuit.Elements.Input {
    class LogicInputElmE : BaseElement {
        public double mHiV;
        public double mLoV;

        public bool Position { get; set; }

        public LogicInputElmE() {
            mHiV = 5;
            mLoV = 0;
        }

        public LogicInputElmE(StringTokenizer st) {
            try {
                mHiV = st.nextTokenDouble();
                mLoV = st.nextTokenDouble();
            } catch {
                mHiV = 5;
                mLoV = 0;
            }
        }

        public override double CirVoltageDiff { get { return CirVolts[0]; } }

        public override int CirVoltageSourceCount { get { return 1; } }

        public override int CirPostCount { get { return 1; } }

        public override bool CirHasGroundConnection(int n1) { return true; }

        public override double CirGetCurrentIntoNode(int n) {
            return -mCirCurrent;
        }

        public override void CirSetCurrent(int vs, double c) { mCirCurrent = -c; }

        public override void CirStamp() {
            double v = Position ? mHiV : mLoV;
            mCir.StampVoltageSource(0, CirNodes[0], mCirVoltSource, v);
        }
    }
}
