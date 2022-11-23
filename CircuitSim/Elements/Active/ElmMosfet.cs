using System;

namespace Circuit.Elements.Active {
    class ElmMosfet : BaseElement {
        const int IdxG = 0;
        const int IdxS = 1;
        const int IdxD = 2;

        public const double DefaultThreshold = 1.5;
        public const double BackwardCompatibilityHfe = 0.02;
        
        public static double LastHfe;

        public static double DefaultHfe {
            get { return LastHfe == 0 ? BackwardCompatibilityHfe : LastHfe; }
        }

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

        ElmDiode mDiodeB1;
        ElmDiode mDiodeB2;
        double[] mLastV = new double[] { 0.0, 0.0, 0.0 };

        public ElmMosfet(bool pnpflag) : base() {
            Pnp = pnpflag ? -1 : 1;
            Hfe = DefaultHfe;
            Vt = DefaultThreshold;
            setupDiodes();
        }
        public ElmMosfet(bool pnpflag, double vt, double hfe) : base() {
            Pnp = pnpflag ? -1 : 1;
            Vt = vt;
            Hfe = hfe;
            setupDiodes();
            AllocNodes();
        }
        /* set up body diodes */
        void setupDiodes() {
            /* diode from node 1 to body terminal */
            mDiodeB1 = new ElmDiode(DiodeModel.GetDefaultModel().Name);
            mDiodeB1.Setup();
            /* diode from node 2 to body terminal */
            mDiodeB2 = new ElmDiode(DiodeModel.GetDefaultModel().Name);
            mDiodeB2.Setup();
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

        public override void Reset() {
            mLastV[1] = mLastV[2] = 0;
            Volts[IdxG] = Volts[IdxS] = Volts[IdxD] = 0;
            CurCount = 0;
            mDiodeB1.ResetDiff();
            mDiodeB2.ResetDiff();
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
            /* limit voltage changes to 0.5V */
            var tmpV = new double[3];
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

            mLastV[IdxG] = tmpV[IdxG];
            mLastV[IdxS] = tmpV[IdxS];
            mLastV[IdxD] = tmpV[IdxD];

            /* if drain < source voltage, swap source and drain.
             * (opposite for PNP) */
            var idxS = IdxS;
            var idxD = IdxD;
            if (Pnp * tmpV[idxD] < Pnp * tmpV[idxS]) {
                idxS = IdxD;
                idxD = IdxS;
            }
            var vgs = tmpV[IdxG] - tmpV[idxS];
            var vds = tmpV[idxD] - tmpV[idxS];
            var realVgs = vgs;
            var realVds = vds;
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

            /* flip ids if we swapped source and drain above */
            var realIds = Ids;
            if (idxS == 2 && Pnp == 1 || idxS == 1 && Pnp == -1) {
                Ids = -Ids;
            }

            var rowD = Circuit.mRowInfo[Nodes[idxD] - 1].MapRow;
            var rowS = Circuit.mRowInfo[Nodes[idxS] - 1].MapRow;
            var colri = Circuit.mRowInfo[Nodes[idxD] - 1];
            if (colri.IsConst) {
                Circuit.mRightSide[rowD] -= Gds * colri.Value;
                Circuit.mRightSide[rowS] += Gds * colri.Value;
            } else {
                Circuit.mMatrix[rowD, colri.MapCol] += Gds;
                Circuit.mMatrix[rowS, colri.MapCol] -= Gds;
            }
            colri = Circuit.mRowInfo[Nodes[IdxG] - 1];
            if (colri.IsConst) {
                Circuit.mRightSide[rowD] -= Gm * colri.Value;
                Circuit.mRightSide[rowS] += Gm * colri.Value;
            } else {
                Circuit.mMatrix[rowD, colri.MapCol] += Gm;
                Circuit.mMatrix[rowS, colri.MapCol] -= Gm;
            }
            colri = Circuit.mRowInfo[Nodes[idxS] - 1];
            if (colri.IsConst) {
                Circuit.mRightSide[rowD] += (Gds + Gm) * colri.Value;
                Circuit.mRightSide[rowS] -= (Gds + Gm) * colri.Value;
            } else {
                Circuit.mMatrix[rowD, colri.MapCol] -= Gds + Gm;
                Circuit.mMatrix[rowS, colri.MapCol] += Gds + Gm;
            }

            var rs = -Pnp * realIds + Gds * realVds + Gm * realVgs;
            rowD = Circuit.mRowInfo[Nodes[idxD] - 1].MapRow;
            rowS = Circuit.mRowInfo[Nodes[idxS] - 1].MapRow;
            Circuit.mRightSide[rowD] += rs;
            Circuit.mRightSide[rowS] -= rs;
        }

        public override void CirIterationFinished() {
            var tmpV = Volts;
            mLastV[IdxG] = tmpV[IdxG];
            mLastV[IdxS] = tmpV[IdxS];
            mLastV[IdxD] = tmpV[IdxD];

            /* if drain < source voltage, swap source and drain.
             * (opposite for PNP) */
            var idxS = IdxS;
            var idxD = IdxD;
            if (Pnp * tmpV[idxD] < Pnp * tmpV[idxS]) {
                idxS = IdxD;
                idxD = IdxS;
            }
            var vgs = tmpV[IdxG] - tmpV[idxS];
            var vds = tmpV[idxD] - tmpV[idxS];
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

            /* flip ids if we swapped source and drain above */
            if (idxS == 2 && Pnp == 1 || idxS == 1 && Pnp == -1) {
                Ids = -Ids;
            }

            /* fix current if body is connected to source or drain */
            if (BodyTerminal == 1) {
                DiodeCurrent1 = -DiodeCurrent2;
            }
            if (BodyTerminal == 2) {
                DiodeCurrent2 = -DiodeCurrent1;
            }
            if (Math.Abs(Ids) > 1e12) {
                Circuit.Stop("Idsが最大電流を超えました", this);
            }
        }
    }
}
