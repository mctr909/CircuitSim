using System;

namespace Circuit.Elements.Active {
    class ElmOpAmp : BaseElement {
        public const int V_N = 0;
        public const int V_P = 1;
        public const int V_O = 2;

        public double MaxOut;
        public double MinOut;
        public double Gain;

        public double Gbw { get; private set; }

        double mLastVd;

        public ElmOpAmp() : base() {
            MaxOut = 15;
            MinOut = -15;
            Gbw = 1e6;
            Gain = 100000;
        }

        public ElmOpAmp(double max, double min, double gbw, double vn, double vp, double gain) : base() {
            MaxOut = max;
            MinOut = min;
            Gbw = gbw;
            Volts[V_N] = vn;
            Volts[V_P] = vp;
            Gain = gain;
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

        public override void CirDoIteration() {
            double vd = Volts[V_P] - Volts[V_N];
            if (Math.Abs(mLastVd - vd) > .1) {
                Circuit.Converged = false;
            } else if (Volts[V_O] > MaxOut + .1 || Volts[V_O] < MinOut - .1) {
                Circuit.Converged = false;
            }
            double x = 0;
            int vn = Circuit.NodeList.Count + mVoltSource;
            double dx = 0;
            if (vd >= MaxOut / Gain && (mLastVd >= 0 || CirSimForm.Random.Next(4) == 1)) {
                dx = 1e-4;
                x = MaxOut - dx * MaxOut / Gain;
            } else if (vd <= MinOut / Gain && (mLastVd <= 0 || CirSimForm.Random.Next(4) == 1)) {
                dx = 1e-4;
                x = MinOut - dx * MinOut / Gain;
            } else {
                dx = Gain;
            }
            /*Console.WriteLine("opamp " + vd + " " + Volts[V_O] + " " + dx + " "  + x + " " + lastvd + " " + Cir.Converged);*/

            /* newton-raphson */
            Circuit.StampMatrix(vn, Nodes[0], dx);
            Circuit.StampMatrix(vn, Nodes[1], -dx);
            Circuit.StampMatrix(vn, Nodes[2], 1);
            Circuit.StampRightSide(vn, x);

            mLastVd = vd;
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
    }
}
