using System;

namespace Circuit.Elements.Active {
    class ElmOpAmp : BaseElement {
        public const int V_N = 0;
        public const int V_P = 1;
        public const int V_O = 2;

        public double MaxOut = 15;
        public double MinOut = -15;
        public double Gain = 100000;

        double mLastVd;

        public ElmOpAmp() : base() { }

        public ElmOpAmp(double max, double min, double gain, double vn, double vp) : base() {
            MaxOut = max;
            MinOut = min;
            Gain = gain;
            Volts[V_N] = vn;
            Volts[V_P] = vp;
        }

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
            double vd = Volts[V_P] - Volts[V_N];
            if (Math.Abs(mLastVd - vd) > 0.1) {
                Circuit.Converged = false;
            }
            else if (Volts[V_O] > MaxOut + 0.1 || Volts[V_O] < MinOut - 0.1) {
                Circuit.Converged = false;
            }
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
            int vn = Circuit.NodeList.Count + mVoltSource;
            Circuit.StampMatrix(vn, Nodes[0], dx);
            Circuit.StampMatrix(vn, Nodes[1], -dx);
            Circuit.StampMatrix(vn, Nodes[2], 1);
            Circuit.StampRightSide(vn, x);

            mLastVd = vd;
        }

    }
}
