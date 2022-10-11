using System;

namespace Circuit.Elements.Active {
    class MosfetElm : BaseElement {
        const int IdxG = 0;
        const int IdxS = 1;
        const int IdxD = 2;

        const double DefaultThreshold = 1.5;
        const double BackwardCompatibilityHfe = 0.02;

        public static double LastHfe;

        public double Vt;
        public double Hfe; /* hfe = 1/(RdsON*(Vgs-Vt)) */
        public bool DoBodyDiode;

        public int Pnp { get; private set; }
        public int BodyTerminal { get; private set; }
        public int Mode { get; private set; } = 0;
        public double Gm { get; private set; } = 0;

        public double Ids { get; private set; }
        public double DiodeCurrent1 { get; private set; }
        public double DiodeCurrent2 { get; private set; }
        public double Vg { get { return Volts[IdxG]; } }
        public double Vs { get { return Volts[IdxS]; } }
        public double Vd { get { return Volts[IdxD]; } }

        double DefaultHfe {
            get { return LastHfe == 0 ? BackwardCompatibilityHfe : LastHfe; }
        }

        Diode mDiodeB1;
        Diode mDiodeB2;
        double[] mLastV = new double[] { 0.0, 0.0, 0.0 };

        public MosfetElm(bool pnpflag) : base() {
            Pnp = pnpflag ? -1 : 1;
            setupDiodes();
            Hfe = DefaultHfe;
            Vt = DefaultThreshold;
        }

        public MosfetElm(bool pnpflag, StringTokenizer st) : base() {
            Pnp = pnpflag ? -1 : 1;
            setupDiodes();
            Vt = DefaultThreshold;
            Hfe = BackwardCompatibilityHfe;
            try {
                Vt = st.nextTokenDouble();
                Hfe = st.nextTokenDouble();
            } catch { }
            AllocNodes(); /* make sure volts[] has the right number of elements when hasBodyTerminal() is true */
        }

        public override double Current { get { return Ids; } }

        public override double VoltageDiff { get { return Volts[IdxD] - Volts[IdxS]; } }

        public override double Power {
            get {
                return Ids * (Volts[IdxD] - Volts[IdxS])
                    - DiodeCurrent1 * (Volts[IdxS] - Volts[BodyTerminal])
                    - DiodeCurrent2 * (Volts[IdxD] - Volts[BodyTerminal]);
            }
        }

        public override bool NonLinear { get { return true; } }

        public override int PostCount { get { return 3; } }

        public override double GetCurrentIntoNode(int n) {
            if (n == 0) {
                return 0;
            }
            if (n == 3) {
                return -DiodeCurrent1 - DiodeCurrent2;
            }
            if (n == 1) {
                return Ids + DiodeCurrent1;
            }
            return -Ids + DiodeCurrent2;
        }

        public override bool AnaGetConnection(int n1, int n2) { return !(n1 == 0 || n2 == 0); }

        public override void AnaStamp() {
            Circuit.StampNonLinear(Nodes[1]);
            Circuit.StampNonLinear(Nodes[2]);

            BodyTerminal = (Pnp == -1) ? 2 : 1;

            if (DoBodyDiode) {
                if (Pnp == -1) {
                    /* pnp: diodes conduct when S or D are higher than body */
                    mDiodeB1.Stamp(Nodes[1], Nodes[BodyTerminal]);
                    mDiodeB2.Stamp(Nodes[2], Nodes[BodyTerminal]);
                } else {
                    /* npn: diodes conduct when body is higher than S or D */
                    mDiodeB1.Stamp(Nodes[BodyTerminal], Nodes[1]);
                    mDiodeB2.Stamp(Nodes[BodyTerminal], Nodes[2]);
                }
            }
        }

        public override void CirDoIteration() {
            calculate(false);
        }

        public override void CirIterationFinished() {
            calculate(true);

            /* fix current if body is connected to source or drain */
            if (BodyTerminal == 1) {
                DiodeCurrent1 = -DiodeCurrent2;
            }
            if (BodyTerminal == 2) {
                DiodeCurrent2 = -DiodeCurrent1;
            }
        }

        public override void Reset() {
            mLastV[1] = mLastV[2] = 0;
            Volts[IdxG] = Volts[IdxS] = Volts[IdxD] = 0;
            CurCount = 0;
            mDiodeB1.Reset();
            mDiodeB2.Reset();
        }

        /* set up body diodes */
        void setupDiodes() {
            /* diode from node 1 to body terminal */
            mDiodeB1 = new Diode();
            mDiodeB1.SetupForDefaultModel();
            /* diode from node 2 to body terminal */
            mDiodeB2 = new Diode();
            mDiodeB2.SetupForDefaultModel();
        }

        /* this is called in doStep to stamp the matrix,
         * and also called in stepFinished() to calculate the current */
        void calculate(bool finished) {
            double[] tmpV;
            if (finished) {
                tmpV = Volts;
            } else {
                /* limit voltage changes to 0.5V */
                tmpV = new double[3];
                tmpV[IdxG] = Volts[IdxG];
                tmpV[IdxS] = Volts[IdxS];
                tmpV[IdxD] = Volts[IdxD];
                if (tmpV[IdxS] > mLastV[IdxS] + 0.5) {
                    tmpV[IdxS] = mLastV[IdxS] + 0.5;
                }
                if (tmpV[IdxS] < mLastV[IdxS] - 0.5) {
                    tmpV[IdxS] = mLastV[IdxS] - 0.5;
                }
                if (tmpV[IdxD] > mLastV[IdxD] + 0.5) {
                    tmpV[IdxD] = mLastV[IdxD] + 0.5;
                }
                if (tmpV[IdxD] < mLastV[IdxD] - 0.5) {
                    tmpV[IdxD] = mLastV[IdxD] - 0.5;
                }
            }

            if (!finished && (nonConvergence(mLastV[IdxS], tmpV[IdxS]) || nonConvergence(mLastV[IdxD], tmpV[IdxD]) || nonConvergence(mLastV[IdxG], tmpV[IdxG]))) {
                Circuit.Converged = false;
            }
            mLastV[IdxG] = tmpV[IdxG];
            mLastV[IdxS] = tmpV[IdxS];
            mLastV[IdxD] = tmpV[IdxD];

            /* if drain < source voltage, swap source and drain.
             * (opposite for PNP) */
            int idxS = IdxS;
            int idxD = IdxD;
            if (Pnp * tmpV[idxD] < Pnp * tmpV[idxS]) {
                idxS = IdxD;
                idxD = IdxS;
            }
            double vgs = tmpV[IdxG] - tmpV[idxS];
            double vds = tmpV[idxD] - tmpV[idxS];
            double realVgs = vgs;
            double realVds = vds;
            vgs *= Pnp;
            vds *= Pnp;

            Ids = 0;
            Gm = 0;
            double Gds = 0;
            if (vgs < Vt) {
                /* should be all zero, but that causes a singular matrix,
                 * so instead we treat it as a large resistor */
                Gds = 1e-8;
                Ids = vds * Gds;
                Mode = 0;
            } else if (vds < vgs - Vt) {
                /* linear */
                Ids = Hfe * ((vgs - Vt) * vds - vds * vds * 0.5);
                Gm = Hfe * vds;
                Gds = Hfe * (vgs - vds - Vt);
                Mode = 1;
            } else {
                /* saturation; Gds = 0 */
                Gm = Hfe * (vgs - Vt);
                /* use very small Gds to avoid nonconvergence */
                Gds = 1e-8;
                Ids = 0.5 * Hfe * (vgs - Vt) * (vgs - Vt) + (vds - (vgs - Vt)) * Gds;
                Mode = 2;
            }

            if (DoBodyDiode) {
                mDiodeB1.CirDoStep(Pnp * (Volts[BodyTerminal] - Volts[IdxS]));
                DiodeCurrent1 = mDiodeB1.CirCalculateCurrent(Pnp * (Volts[BodyTerminal] - Volts[IdxS])) * Pnp;
                mDiodeB2.CirDoStep(Pnp * (Volts[BodyTerminal] - Volts[IdxD]));
                DiodeCurrent2 = mDiodeB2.CirCalculateCurrent(Pnp * (Volts[BodyTerminal] - Volts[IdxD])) * Pnp;
            } else {
                DiodeCurrent1 = DiodeCurrent2 = 0;
            }

            double ids0 = Ids;

            /* flip ids if we swapped source and drain above */
            if (idxS == 2 && Pnp == 1 || idxS == 1 && Pnp == -1) {
                Ids = -Ids;
            }

            if (finished) {
                return;
            }

            Circuit.StampMatrix(Nodes[idxD], Nodes[idxD], Gds);
            Circuit.StampMatrix(Nodes[idxD], Nodes[idxS], -Gds - Gm);
            Circuit.StampMatrix(Nodes[idxD], Nodes[IdxG], Gm);
            Circuit.StampMatrix(Nodes[idxS], Nodes[idxD], -Gds);
            Circuit.StampMatrix(Nodes[idxS], Nodes[idxS], Gds + Gm);
            Circuit.StampMatrix(Nodes[idxS], Nodes[IdxG], -Gm);

            double rs = -Pnp * ids0 + Gds * realVds + Gm * realVgs;
            Circuit.StampRightSide(Nodes[idxD], rs);
            Circuit.StampRightSide(Nodes[idxS], -rs);
        }

        bool nonConvergence(double last, double now) {
            double diff = Math.Abs(last - now);

            /* high beta MOSFETs are more sensitive to small differences,
             * so we are more strict about convergence testing */
            if (Hfe > 1) {
                diff *= 100;
            }

            /* difference of less than 10mV is fine */
            if (diff < .01) {
                return false;
            }
            /* larger differences are fine if value is large */
            if (Circuit.SubIterations > 10 && diff < Math.Abs(now) * .001) {
                return false;
            }
            /* if we're having trouble converging, get more lenient */
            if (Circuit.SubIterations > 100 && diff < .01 + (Circuit.SubIterations - 100) * .0001) {
                return false;
            }
            return true;
        }
    }
}
