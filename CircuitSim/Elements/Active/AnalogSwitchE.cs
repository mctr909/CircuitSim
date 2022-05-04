namespace Circuit.Elements.Active {
    class AnalogSwitchElmE : BaseElement {
        public double Ron;
        public double Roff;
        public bool Invert;

        public bool IsOpen { get; private set; }

        double mResistance;

        public AnalogSwitchElmE() : base() {
            Ron = 20;
            Roff = 1e10;
        }

        public AnalogSwitchElmE(StringTokenizer st) : base() {
            Ron = 20;
            Roff = 1e10;
            try {
                Ron = double.Parse(st.nextToken());
                Roff = double.Parse(st.nextToken());
            } catch { }
        }

        protected override void cirCalculateCurrent() {
            mCirCurrent = (CirVolts[0] - CirVolts[1]) / mResistance;
        }

        // we need this to be able to change the matrix for each step
        public override bool CirNonLinear { get { return true; } }

        public override int CirPostCount { get { return 3; } }

        public override double CirGetCurrentIntoNode(int n) {
            if (n == 0) {
                return -mCirCurrent;
            }
            if (n == 2) {
                return 0;
            }
            return mCirCurrent;
        }

        public override void CirStamp() {
            mCir.StampNonLinear(CirNodes[0]);
            mCir.StampNonLinear(CirNodes[1]);
        }

        public override void CirDoStep() {
            IsOpen = CirVolts[2] < 2.5;
            if (Invert) {
                IsOpen = !IsOpen;
            }
            mResistance = IsOpen ? Roff : Ron;
            mCir.StampResistor(CirNodes[0], CirNodes[1], mResistance);
        }
    }
}
