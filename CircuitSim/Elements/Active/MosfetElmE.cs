using System;

namespace Circuit.Elements.Active {
    class MosfetElmE : BaseElement {
        public const int V_G = 0;
        public const int V_S = 1;
        public const int V_D = 2;

        const double DefaultThreshold = 1.5;
        const double BackwardCompatibilityHfe = 0.02;

        public static double LastHfe;

        public double Vt;
        public double Hfe; /* hfe = 1/(RdsON*(Vgs-Vt)) */
        public bool DoBodyDiode;

        public int Pnp { get; private set; }
        public int BodyTerminal { get; private set; }
        public double DiodeCurrent1 { get; private set; }
        public double DiodeCurrent2 { get; private set; }
        public double Ids { get; private set; }
        public int Mode { get; private set; } = 0;
        public double Gm { get; private set; } = 0;

        double DefaultHfe {
            get { return LastHfe == 0 ? BackwardCompatibilityHfe : LastHfe; }
        }

        Diode mDiodeB1;
        Diode mDiodeB2;
        double mLastV0;
        double mLastV1;
        double mLastV2;

        public MosfetElmE(bool pnpflag) : base() {
            Pnp = pnpflag ? -1 : 1;
            setupDiodes();
            Hfe = DefaultHfe;
            Vt = DefaultThreshold;
        }

        public MosfetElmE(bool pnpflag, StringTokenizer st) : base() {
            Pnp = pnpflag ? -1 : 1;
            setupDiodes();
            Vt = DefaultThreshold;
            Hfe = BackwardCompatibilityHfe;
            try {
                Vt = st.nextTokenDouble();
                Hfe = st.nextTokenDouble();
            } catch { }
            cirAllocNodes(); /* make sure volts[] has the right number of elements when hasBodyTerminal() is true */
        }

        public override double CirCurrent { get { return Ids; } }

        public override double CirVoltageDiff { get { return CirVolts[V_D] - CirVolts[V_S]; } }

        public override double CirPower {
            get {
                return Ids * (CirVolts[V_D] - CirVolts[V_S])
                    - DiodeCurrent1 * (CirVolts[V_S] - CirVolts[BodyTerminal])
                    - DiodeCurrent2 * (CirVolts[V_D] - CirVolts[BodyTerminal]);
            }
        }

        public override bool CirNonLinear { get { return true; } }

        public override int CirPostCount { get { return 3; } }

        public override double CirGetCurrentIntoNode(int n) {
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

        public override void CirStamp() {
            mCir.StampNonLinear(CirNodes[1]);
            mCir.StampNonLinear(CirNodes[2]);

            BodyTerminal = (Pnp == -1) ? 2 : 1;

            if (DoBodyDiode) {
                if (Pnp == -1) {
                    /* pnp: diodes conduct when S or D are higher than body */
                    mDiodeB1.Stamp(CirNodes[1], CirNodes[BodyTerminal]);
                    mDiodeB2.Stamp(CirNodes[2], CirNodes[BodyTerminal]);
                } else {
                    /* npn: diodes conduct when body is higher than S or D */
                    mDiodeB1.Stamp(CirNodes[BodyTerminal], CirNodes[1]);
                    mDiodeB2.Stamp(CirNodes[BodyTerminal], CirNodes[2]);
                }
            }
        }

        public override void CirDoStep() {
            calculate(false);
        }

        public override void CirStepFinished() {
            calculate(true);

            /* fix current if body is connected to source or drain */
            if (BodyTerminal == 1) {
                DiodeCurrent1 = -DiodeCurrent2;
            }
            if (BodyTerminal == 2) {
                DiodeCurrent2 = -DiodeCurrent1;
            }
        }

        public override void CirReset() {
            mLastV1 = mLastV2 = 0;
            CirVolts[V_G] = CirVolts[V_S] = CirVolts[V_D] = 0;
            mCirCurCount = 0;
            mDiodeB1.Reset();
            mDiodeB2.Reset();
        }

        /* set up body diodes */
        void setupDiodes() {
            /* diode from node 1 to body terminal */
            mDiodeB1 = new Diode(mCir);
            mDiodeB1.SetupForDefaultModel();
            /* diode from node 2 to body terminal */
            mDiodeB2 = new Diode(mCir);
            mDiodeB2.SetupForDefaultModel();
        }

        /* this is called in doStep to stamp the matrix,
         * and also called in stepFinished() to calculate the current */
        void calculate(bool finished) {
            double[] vs;
            if (finished) {
                vs = CirVolts;
            } else {
                /* limit voltage changes to .5V */
                vs = new double[3];
                vs[0] = CirVolts[V_G];
                vs[1] = CirVolts[V_S];
                vs[2] = CirVolts[V_D];
                if (vs[1] > mLastV1 + .5) {
                    vs[1] = mLastV1 + .5;
                }
                if (vs[1] < mLastV1 - .5) {
                    vs[1] = mLastV1 - .5;
                }
                if (vs[2] > mLastV2 + .5) {
                    vs[2] = mLastV2 + .5;
                }
                if (vs[2] < mLastV2 - .5) {
                    vs[2] = mLastV2 - .5;
                }
            }

            int source = 1;
            int drain = 2;

            /* if source voltage > drain (for NPN), swap source and drain
             * (opposite for PNP) */
            if (Pnp * vs[1] > Pnp * vs[2]) {
                source = 2;
                drain = 1;
            }
            int gate = 0;
            double vgs = vs[gate] - vs[source];
            double vds = vs[drain] - vs[source];
            if (!finished && (nonConvergence(mLastV1, vs[1]) || nonConvergence(mLastV2, vs[2]) || nonConvergence(mLastV0, vs[0]))) {
                mCir.Converged = false;
            }
            mLastV0 = vs[0];
            mLastV1 = vs[1];
            mLastV2 = vs[2];
            double realvgs = vgs;
            double realvds = vds;
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
                Ids = Hfe * ((vgs - Vt) * vds - vds * vds * .5);
                Gm = Hfe * vds;
                Gds = Hfe * (vgs - vds - Vt);
                Mode = 1;
            } else {
                /* saturation; Gds = 0 */
                Gm = Hfe * (vgs - Vt);
                /* use very small Gds to avoid nonconvergence */
                Gds = 1e-8;
                Ids = .5 * Hfe * (vgs - Vt) * (vgs - Vt) + (vds - (vgs - Vt)) * Gds;
                Mode = 2;
            }

            if (DoBodyDiode) {
                mDiodeB1.DoStep(Pnp * (CirVolts[BodyTerminal] - CirVolts[V_S]));
                DiodeCurrent1 = mDiodeB1.CalculateCurrent(Pnp * (CirVolts[BodyTerminal] - CirVolts[V_S])) * Pnp;
                mDiodeB2.DoStep(Pnp * (CirVolts[BodyTerminal] - CirVolts[V_D]));
                DiodeCurrent2 = mDiodeB2.CalculateCurrent(Pnp * (CirVolts[BodyTerminal] - CirVolts[V_D])) * Pnp;
            } else {
                DiodeCurrent1 = DiodeCurrent2 = 0;
            }

            double ids0 = Ids;

            /* flip ids if we swapped source and drain above */
            if (source == 2 && Pnp == 1 || source == 1 && Pnp == -1) {
                Ids = -Ids;
            }

            if (finished) {
                return;
            }

            double rs = -Pnp * ids0 + Gds * realvds + Gm * realvgs;
            mCir.StampMatrix(CirNodes[drain], CirNodes[drain], Gds);
            mCir.StampMatrix(CirNodes[drain], CirNodes[source], -Gds - Gm);
            mCir.StampMatrix(CirNodes[drain], CirNodes[gate], Gm);

            mCir.StampMatrix(CirNodes[source], CirNodes[drain], -Gds);
            mCir.StampMatrix(CirNodes[source], CirNodes[source], Gds + Gm);
            mCir.StampMatrix(CirNodes[source], CirNodes[gate], -Gm);

            mCir.StampRightSide(CirNodes[drain], rs);
            mCir.StampRightSide(CirNodes[source], -rs);
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
            if (mCir.SubIterations > 10 && diff < Math.Abs(now) * .001) {
                return false;
            }
            /* if we're having trouble converging, get more lenient */
            if (mCir.SubIterations > 100 && diff < .01 + (mCir.SubIterations - 100) * .0001) {
                return false;
            }
            return true;
        }
    }
}
