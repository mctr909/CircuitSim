using System.Drawing;

namespace Circuit.Elements.Gate {
    class ElmTriState : BaseElement {
        double mResistance;
        public double Ron;
        public double Roff;

        public bool Open { get; private set; }

        public ElmTriState() : base() {
            Ron = 0.1;
            Roff = 1e10;
        }

        public ElmTriState(StringTokenizer st) : base() {
            Ron = st.nextTokenDouble(0.1);
            Roff = st.nextTokenDouble(1e10);
        }

        public override int PostCount { get { return 3; } }

        public override int AnaInternalNodeCount { get { return 1; } }

        public override int AnaVoltageSourceCount { get { return 1; } }

        public override double CirGetCurrentIntoNode(int n) {
            if (n == 1) {
                return Current;
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

        public override void CirDoIteration() {
            Open = Volts[2] < 2.5;
            mResistance = Open ? Roff : Ron;
            Circuit.StampResistor(Nodes[3], Nodes[1], mResistance);
            Circuit.UpdateVoltageSource(mVoltSource, Volts[0] > 2.5 ? 5 : 0);
        }

        public override void CirSetVoltage(int n, double c) {
            Volts[n] = c;
            Current = (Volts[0] - Volts[1]) / mResistance;
        }
    }
}
