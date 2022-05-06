﻿namespace Circuit.Elements.Gate {
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

        /* there is no current path through the input, but there
         * is an indirect path through the output to ground. */
        public override bool GetConnection(int n1, int n2) { return false; }

        public override double GetCurrentIntoNode(int n) {
            if (n == 1) {
                return mCurrent;
            }
            return 0;
        }

        public override bool AnaHasGroundConnection(int n1) {
            return n1 == 1;
        }

        public override void AnaStamp() {
            mCir.StampVoltageSource(0, Nodes[3], mVoltSource);
            mCir.StampNonLinear(Nodes[3]);
            mCir.StampNonLinear(Nodes[1]);
        }

        public override void CirDoStep() {
            Open = Volts[2] < 2.5;
            mResistance = Open ? Roff : Ron;
            mCir.StampResistor(Nodes[3], Nodes[1], mResistance);
            mCir.UpdateVoltageSource(0, Nodes[3], mVoltSource, Volts[0] > 2.5 ? 5 : 0);
        }

        public override void CirSetNodeVoltage(int n, double c) {
            Volts[n] = c;
            mCurrent = (Volts[0] - Volts[1]) / mResistance;
        }
    }
}
