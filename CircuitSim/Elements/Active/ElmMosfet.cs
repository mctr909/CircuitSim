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
            /* diode from node 2 to body terminal */
            mDiodeB2 = new ElmDiode(DiodeModel.GetDefaultModel().Name);
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
            Circuit.StampNonLinear(Nodes[IdxS]);
            Circuit.StampNonLinear(Nodes[IdxD]);

            BodyTerminal = (Pnp == -1) ? IdxD : IdxS;

            if (DoBodyDiode) {
                var ns = Nodes[IdxS];
                var nd = Nodes[IdxD];
                int nx;
                if (Pnp == -1) {
                    /* pnp: diodes conduct when S or D are higher than body */
                    nx = nd;
                } else {
                    /* npn: diodes conduct when body is higher than S or D */
                    nx = ns;
                }
                mDiodeB1.Stamp(ns, nx);
                mDiodeB2.Stamp(nx, nd);
            }
        }

        public override void CirDoIteration() {
            /* limit voltage changes to 0.5V */
            var tmpVg = Volts[IdxG];
            var tmpVs = Volts[IdxS];
            var tmpVd = Volts[IdxD];
            if (tmpVs > mLastV[IdxS] + 0.5) {
                tmpVs = mLastV[IdxS] + 0.5;
            }
            if (tmpVs < mLastV[IdxS] - 0.5) {
                tmpVs = mLastV[IdxS] - 0.5;
            }
            if (tmpVd > mLastV[IdxD] + 0.5) {
                tmpVd = mLastV[IdxD] + 0.5;
            }
            if (tmpVd < mLastV[IdxD] - 0.5) {
                tmpVd = mLastV[IdxD] - 0.5;
            }

            mLastV[IdxG] = tmpVg;
            mLastV[IdxS] = tmpVs;
            mLastV[IdxD] = tmpVd;

            /* if drain < source voltage, swap source and drain.
             * (opposite for PNP) */
            int idxS, idxD;
            if (Pnp * tmpVd < Pnp * tmpVs) {
                idxS = IdxD;
                idxD = IdxS;
            } else {
                idxS = IdxS;
                idxD = IdxD;
            }

            var vgs = tmpVg - tmpVs;
            var vds = tmpVd - tmpVs;
            var realVgs = vgs;
            var realVds = vds;
            vgs *= Pnp;
            vds *= Pnp;

            double Gds;
            if (vgs < Vt) {
                /* mode: off */
                /* should be all zero, but that causes a singular matrix,
                 * so instead we treat it as a large resistor */
                Gds = 1e-8;
                Ids = vds * Gds;
                Gm = 0;
                Mode = 0;
            } else if (vds < vgs - Vt) {
                /* mode: linear */
                Ids = Hfe * ((vgs - Vt) * vds - vds * vds * 0.5);
                Gm = Hfe * vds;
                Gds = Hfe * (vgs - vds - Vt);
                Mode = 1;
            } else {
                /* mode: saturation */
                Gm = Hfe * (vgs - Vt);
                /* use very small Gds to avoid nonconvergence */
                Gds = 1e-8;
                Ids = 0.5 * Hfe * (vgs - Vt) * (vgs - Vt) + (vds - (vgs - Vt)) * Gds;
                Mode = 2;
            }

            if (DoBodyDiode) {
                var vbs = (Volts[BodyTerminal] - Volts[IdxS]) * Pnp;
                var vbd = (Volts[BodyTerminal] - Volts[IdxD]) * Pnp;
                mDiodeB1.CirDoStep(vbs);
                DiodeCurrent1 = mDiodeB1.CirCalculateCurrent(vbs) * Pnp;
                mDiodeB2.CirDoStep(vbd);
                DiodeCurrent2 = mDiodeB2.CirCalculateCurrent(vbd) * Pnp;
            } else {
                DiodeCurrent1 = DiodeCurrent2 = 0;
            }

            /* flip ids if we swapped source and drain above */
            var realIds = Ids;
            if (idxS == 2 && Pnp == 1 || idxS == 1 && Pnp == -1) {
                Ids = -Ids;
            }

            var rowD = Circuit.RowInfo[Nodes[idxD] - 1].MapRow;
            var rowS = Circuit.RowInfo[Nodes[idxS] - 1].MapRow;
            var colri = Circuit.RowInfo[Nodes[idxD] - 1];
            if (colri.IsConst) {
                Circuit.RightSide[rowD] -= Gds * colri.Value;
                Circuit.RightSide[rowS] += Gds * colri.Value;
            } else {
                Circuit.Matrix[rowD, colri.MapCol] += Gds;
                Circuit.Matrix[rowS, colri.MapCol] -= Gds;
            }
            colri = Circuit.RowInfo[Nodes[IdxG] - 1];
            if (colri.IsConst) {
                Circuit.RightSide[rowD] -= Gm * colri.Value;
                Circuit.RightSide[rowS] += Gm * colri.Value;
            } else {
                Circuit.Matrix[rowD, colri.MapCol] += Gm;
                Circuit.Matrix[rowS, colri.MapCol] -= Gm;
            }
            colri = Circuit.RowInfo[Nodes[idxS] - 1];
            if (colri.IsConst) {
                Circuit.RightSide[rowD] += (Gds + Gm) * colri.Value;
                Circuit.RightSide[rowS] -= (Gds + Gm) * colri.Value;
            } else {
                Circuit.Matrix[rowD, colri.MapCol] -= Gds + Gm;
                Circuit.Matrix[rowS, colri.MapCol] += Gds + Gm;
            }

            var rs = -Pnp * realIds + Gds * realVds + Gm * realVgs;
            rowD = Circuit.RowInfo[Nodes[idxD] - 1].MapRow;
            rowS = Circuit.RowInfo[Nodes[idxS] - 1].MapRow;
            Circuit.RightSide[rowD] += rs;
            Circuit.RightSide[rowS] -= rs;
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
                var vbs = (Volts[BodyTerminal] - Volts[IdxS]) * Pnp;
                var vbd = (Volts[BodyTerminal] - Volts[IdxD]) * Pnp;
                mDiodeB1.CirDoStep(vbs);
                DiodeCurrent1 = mDiodeB1.CirCalculateCurrent(vbs) * Pnp;
                mDiodeB2.CirDoStep(vbd);
                DiodeCurrent2 = mDiodeB2.CirCalculateCurrent(vbd) * Pnp;
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
            if (Math.Abs(vds) > 1e4) {
                Circuit.Stop("Vdsが最大を超えました", this);
            }
            if (Math.Abs(Ids) > 1e3) {
                Circuit.Stop("Idsが最大を超えました", this);
            }
        }
    }
}
