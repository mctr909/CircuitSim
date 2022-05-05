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

        protected override void calcCurrent() {
            mCurrent = (Volts[0] - Volts[1]) / mResistance;
        }

        public override void Stamp() {
            mCir.StampVoltageSource(0, Nodes[3], mVoltSource);
            mCir.StampNonLinear(Nodes[3]);
            mCir.StampNonLinear(Nodes[1]);
        }

        public override void DoStep() {
            Open = Volts[2] < 2.5;
            mResistance = Open ? Roff : Ron;
            mCir.StampResistor(Nodes[3], Nodes[1], mResistance);
            mCir.UpdateVoltageSource(0, Nodes[3], mVoltSource, Volts[0] > 2.5 ? 5 : 0);
        }

        public override bool HasGroundConnection(int n1) {
            return n1 == 1;
        }
    }
}
