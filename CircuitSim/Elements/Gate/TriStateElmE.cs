namespace Circuit.Elements.Gate {
    class TriStateElmE : BaseElement {
        double mResistance;
        public double Ron;
        public double Roff;

        public bool Open { get; private set; }

        public TriStateElmE() : base() {
            Ron = 0.1;
            Roff = 1e10;
        }

        public TriStateElmE(StringTokenizer st) : base() {
            Ron = 0.1;
            Roff = 1e10;
            try {
                Ron = st.nextTokenDouble();
                Roff = st.nextTokenDouble();
            } catch { }
        }

        /* we need this to be able to change the matrix for each step */
        public override bool CirNonLinear {
            get { return true; }
        }

        public override int CirPostCount { get { return 3; } }

        public override int CirInternalNodeCount { get { return 1; } }

        public override int CirVoltageSourceCount { get { return 1; } }

        public override double CirGetCurrentIntoNode(int n) {
            if (n == 1) {
                return mCirCurrent;
            }
            return 0;
        }

        protected override void cirCalculateCurrent() {
            mCirCurrent = (CirVolts[0] - CirVolts[1]) / mResistance;
        }

        public override void CirStamp() {
            mCir.StampVoltageSource(0, CirNodes[3], mCirVoltSource);
            mCir.StampNonLinear(CirNodes[3]);
            mCir.StampNonLinear(CirNodes[1]);
        }

        public override void CirDoStep() {
            Open = CirVolts[2] < 2.5;
            mResistance = Open ? Roff : Ron;
            mCir.StampResistor(CirNodes[3], CirNodes[1], mResistance);
            mCir.UpdateVoltageSource(0, CirNodes[3], mCirVoltSource, CirVolts[0] > 2.5 ? 5 : 0);
        }

        public override bool CirHasGroundConnection(int n1) {
            return n1 == 1;
        }
    }
}
