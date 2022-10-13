namespace Circuit.Elements.Active {
    class ElmAnalogSwitch : BaseElement {
        public double Ron = 20;
        public double Roff = 1e10;
        public bool Invert;

        double mResistance;

        public bool IsOpen { get; private set; }

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
            var conductance = 1.0 / mResistance;
            var rowA = Circuit.mRowInfo[Nodes[0] - 1].MapRow;
            var rowB = Circuit.mRowInfo[Nodes[1] - 1].MapRow;
            var colri = Circuit.mRowInfo[Nodes[0] - 1];
            if (colri.IsConst) {
                Circuit.mRightSide[rowA] -= conductance * colri.Value;
                Circuit.mRightSide[rowB] += conductance * colri.Value;
            } else {
                Circuit.mMatrix[rowA, colri.MapCol] += conductance;
                Circuit.mMatrix[rowB, colri.MapCol] -= conductance;
            }
            colri = Circuit.mRowInfo[Nodes[1] - 1];
            if (colri.IsConst) {
                Circuit.mRightSide[rowA] += conductance * colri.Value;
                Circuit.mRightSide[rowB] -= conductance * colri.Value;
            } else {
                Circuit.mMatrix[rowA, colri.MapCol] -= conductance;
                Circuit.mMatrix[rowB, colri.MapCol] += conductance;
            }
        }

        public override void CirSetVoltage(int n, double c) {
            Volts[n] = c;
            mCurrent = (Volts[0] - Volts[1]) / mResistance;
        }
    }
}
