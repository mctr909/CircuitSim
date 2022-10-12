namespace Circuit.Elements.Active {
    class ElmAnalogSwitch : BaseElement {
        public double Ron = 20;
        public double Roff = 1e10;
        public bool Invert;

        public bool IsOpen { get; private set; }

        double mResistance;

        // we need this to be able to change the matrix for each step
        public override bool NonLinear { get { return true; } }

        public override int PostCount { get { return 3; } }

        public override double GetCurrentIntoNode(int n) {
            if (n == 0) {
                return -mCurrent;
            }
            if (n == 2) {
                return 0;
            }
            return mCurrent;
        }

        // we have to just assume current will flow either way, even though that
        // might cause singular matrix errors
        public override bool AnaGetConnection(int n1, int n2) { return !(n1 == 2 || n2 == 2); }

        public override void AnaStamp() {
            Circuit.StampNonLinear(Nodes[0]);
            Circuit.StampNonLinear(Nodes[1]);
        }

        public override void CirDoIteration() {
            IsOpen = Volts[2] < 2.5;
            if (Invert) {
                IsOpen = !IsOpen;
            }
            mResistance = IsOpen ? Roff : Ron;
            Circuit.StampResistor(Nodes[0], Nodes[1], mResistance);
        }

        public override void CirSetVoltage(int n, double c) {
            Volts[n] = c;
            mCurrent = (Volts[0] - Volts[1]) / mResistance;
        }
    }
}
