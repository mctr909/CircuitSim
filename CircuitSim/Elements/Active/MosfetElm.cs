using System;

namespace Circuit.Elements.Active {
    class MosfetElm : BaseElement {
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

        public override double VoltageDiff { get { return Volts[V_D] - Volts[V_S]; } }

        public override double Power {
            get {
                return Ids * (Volts[V_D] - Volts[V_S])
                    - DiodeCurrent1 * (Volts[V_S] - Volts[BodyTerminal])
                    - DiodeCurrent2 * (Volts[V_D] - Volts[BodyTerminal]);
            }
        }

        public override bool NonLinear { get { return true; } }

        public override int PostCount { get { return 3; } }

        public override bool GetConnection(int n1, int n2) { return !(n1 == 0 || n2 == 0); }

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

        public override void Reset() {
            mLastV1 = mLastV2 = 0;
            Volts[V_G] = Volts[V_S] = Volts[V_D] = 0;
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
            double[] vs;
            if (finished) {
                vs = Volts;
            } else {
                /* limit voltage changes to .5V */
                vs = new double[3];
                vs[0] = Volts[V_G];
                vs[1] = Volts[V_S];
                vs[2] = Volts[V_D];
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
                Circuit.Converged = false;
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
                mDiodeB1.DoStep(Pnp * (Volts[BodyTerminal] - Volts[V_S]));
                DiodeCurrent1 = mDiodeB1.CalculateCurrent(Pnp * (Volts[BodyTerminal] - Volts[V_S])) * Pnp;
                mDiodeB2.DoStep(Pnp * (Volts[BodyTerminal] - Volts[V_D]));
                DiodeCurrent2 = mDiodeB2.CalculateCurrent(Pnp * (Volts[BodyTerminal] - Volts[V_D])) * Pnp;
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
            Circuit.StampMatrix(Nodes[drain], Nodes[drain], Gds);
            Circuit.StampMatrix(Nodes[drain], Nodes[source], -Gds - Gm);
            Circuit.StampMatrix(Nodes[drain], Nodes[gate], Gm);

            Circuit.StampMatrix(Nodes[source], Nodes[drain], -Gds);
            Circuit.StampMatrix(Nodes[source], Nodes[source], Gds + Gm);
            Circuit.StampMatrix(Nodes[source], Nodes[gate], -Gm);

            Circuit.StampRightSide(Nodes[drain], rs);
            Circuit.StampRightSide(Nodes[source], -rs);
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
