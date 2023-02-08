namespace Circuit.Elements.Active {
    class ElmOpAmp : BaseElement {
        public const int V_N = 0;
        public const int V_P = 1;
        public const int V_O = 2;

        public double MaxOut = 15;
        public double MinOut = -15;
        public double Gain = 100000;

        double mLastVd;

        public override double VoltageDiff { get { return Volts[V_O] - Volts[V_P]; } }

        public override double Power { get { return Volts[V_O] * mCurrent; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override bool NonLinear { get { return true; } }

        public override int PostCount { get { return 3; } }

        public override double GetCurrentIntoNode(int n) {
            if (n == 2) {
                return -mCurrent;
            }
            return 0;
        }

        /* there is no current path through the op-amp inputs,
         * but there is an indirect path through the output to ground. */
        public override bool AnaGetConnection(int n1, int n2) { return false; }

        public override void AnaStamp() {
            int vn = Circuit.NodeList.Count + mVoltSource;
            Circuit.StampNonLinear(vn);
            Circuit.StampMatrix(Nodes[2], vn, 1);
        }

        public override bool AnaHasGroundConnection(int n1) { return n1 == 2; }

        public override void CirDoIteration() {
            var vd = Volts[V_P] - Volts[V_N];
            double dx;
            double x;
            if (vd >= MaxOut / Gain && (mLastVd >= 0 || CirSimForm.Random.Next(4) == 1)) {
                dx = 1e-4;
                x = MaxOut - dx * MaxOut / Gain;
            }
            else if (vd <= MinOut / Gain && (mLastVd <= 0 || CirSimForm.Random.Next(4) == 1)) {
                dx = 1e-4;
                x = MinOut - dx * MinOut / Gain;
            }
            else {
                dx = Gain;
                x = 0;
            }

            /* newton-raphson */
            var vnode = Circuit.NodeList.Count + mVoltSource;
            var rowV = Circuit.RowInfo[vnode - 1].MapRow;
            var colri = Circuit.RowInfo[Nodes[0] - 1];
            if (colri.IsConst) {
                Circuit.RightSide[rowV] -= dx * colri.Value;
            } else {
                Circuit.Matrix[rowV, colri.MapCol] += dx;
            }
            colri = Circuit.RowInfo[Nodes[1] - 1];
            if (colri.IsConst) {
                Circuit.RightSide[rowV] += dx * colri.Value;
            } else {
                Circuit.Matrix[rowV, colri.MapCol] -= dx;
            }
            colri = Circuit.RowInfo[Nodes[2] - 1];
            if (colri.IsConst) {
                Circuit.RightSide[rowV] -= colri.Value;
            } else {
                Circuit.Matrix[rowV, colri.MapCol] += 1;
            }
            Circuit.RightSide[rowV] += x;

            mLastVd = vd;
        }
    }
}
