namespace Circuit.Elements.Gate {
    class TriStateElm : BaseElement {
        double mResistance;
        public double Ron;
        public double Roff;

        public bool Open { get; private set; }

        public TriStateElm() : base() {
            Ron = 0.1;
            Roff = 1e10;
        }

        public TriStateElm(StringTokenizer st) : base() {
            Ron = 0.1;
            Roff = 1e10;
            try {
                Ron = st.nextTokenDouble();
                Roff = st.nextTokenDouble();
            } catch { }
        }

        /* we need this to be able to change the matrix for each step */
        public override bool NonLinear {
            get { return true; }
        }

        public override int PostCount { get { return 3; } }

        public override int InternalNodeCount { get { return 1; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override double GetCurrentIntoNode(int n) {
            if (n == 1) {
                return mCurrent;
            }
            return 0;
        }

        /* there is no current path through the input, but there
         * is an indirect path through the output to ground. */
        public override bool AnaGetConnection(int n1, int n2) { return false; }

        public override bool AnaHasGroundConnection(int n1) {
            return n1 == 1;
        }

        public override void AnaStamp() {
            Circuit.StampVoltageSource(0, Nodes[3], mVoltSource);
            Circuit.StampNonLinear(Nodes[3]);
            Circuit.StampNonLinear(Nodes[1]);
        }

        public override void CirDoStep() {
            Open = Volts[2] < 2.5;
            mResistance = Open ? Roff : Ron;
            Circuit.StampResistor(Nodes[3], Nodes[1], mResistance);
            Circuit.UpdateVoltageSource(0, Nodes[3], mVoltSource, Volts[0] > 2.5 ? 5 : 0);
        }

        public override void CirSetNodeVoltage(int n, double c) {
            Volts[n] = c;
            mCurrent = (Volts[0] - Volts[1]) / mResistance;
        }
    }
}
