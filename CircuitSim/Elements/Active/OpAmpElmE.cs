using System;

namespace Circuit.Elements.Active {
    class OpAmpElmE : BaseElement {
        public const int V_N = 0;
        public const int V_P = 1;
        public const int V_O = 2;

        public double MaxOut;
        public double MinOut;
        public double Gain;

        public double Gbw { get; private set; }

        double mLastVd;

        public OpAmpElmE() : base() {
            MaxOut = 15;
            MinOut = -15;
            Gbw = 1e6;
            Gain = 100000;
        }

        public OpAmpElmE(StringTokenizer st) : base() {
            /* GBW has no effect in this version of the simulator,
             * but we retain it to keep the file format the same */
            try {
                MaxOut = st.nextTokenDouble();
                MinOut = st.nextTokenDouble();
                Gbw = st.nextTokenDouble();
                Volts[V_N] = st.nextTokenDouble();
                Volts[V_P] = st.nextTokenDouble();
                Gain = st.nextTokenDouble();
            } catch {
                MaxOut = 15;
                MinOut = -15;
                Gbw = 1e6;
            }
        }

        public override double VoltageDiff { get { return Volts[V_O] - Volts[V_P]; } }

        public override double Power { get { return Volts[V_O] * mCurrent; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override bool NonLinear { get { return true; } }

        public override int PostCount { get { return 3; } }

        public override void Stamp() {
            int vn = mCir.NodeList.Count + mVoltSource;
            mCir.StampNonLinear(vn);
            mCir.StampMatrix(Nodes[2], vn, 1);
        }

        public override void DoStep() {
            double vd = Volts[V_P] - Volts[V_N];
            if (Math.Abs(mLastVd - vd) > .1) {
                mCir.Converged = false;
            } else if (Volts[V_O] > MaxOut + .1 || Volts[V_O] < MinOut - .1) {
                mCir.Converged = false;
            }
            double x = 0;
            int vn = mCir.NodeList.Count + mVoltSource;
            double dx = 0;
            if (vd >= MaxOut / Gain && (mLastVd >= 0 || CirSim.Random.Next(4) == 1)) {
                dx = 1e-4;
                x = MaxOut - dx * MaxOut / Gain;
            } else if (vd <= MinOut / Gain && (mLastVd <= 0 || CirSim.Random.Next(4) == 1)) {
                dx = 1e-4;
                x = MinOut - dx * MinOut / Gain;
            } else {
                dx = Gain;
            }
            /*Console.WriteLine("opamp " + vd + " " + Volts[V_O] + " " + dx + " "  + x + " " + lastvd + " " + Cir.Converged);*/

            /* newton-raphson */
            mCir.StampMatrix(vn, Nodes[0], dx);
            mCir.StampMatrix(vn, Nodes[1], -dx);
            mCir.StampMatrix(vn, Nodes[2], 1);
            mCir.StampRightSide(vn, x);

            mLastVd = vd;
        }

        public override bool HasGroundConnection(int n1) { return n1 == 2; }

        public override double GetCurrentIntoNode(int n) {
            if (n == 2) {
                return -mCurrent;
            }
            return 0;
        }
    }
}
