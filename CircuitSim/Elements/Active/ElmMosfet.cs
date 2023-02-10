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

        public double DiodeCurrent1 { get; private set; }
        public double DiodeCurrent2 { get; private set; }
        public double Vg { get { return Volts[IdxG]; } }
        public double Vs { get { return Volts[IdxS]; } }
        public double Vd { get { return Volts[IdxD]; } }

        const double DiodeVcrit = 0.6347668814648425;
        const double DiodeVscale = 0.05173;
        const double DiodeLeakage = 1.7143528192808883E-07;
        const double DiodeVdCoef = 19.331142470520007;
        int mDiode1Node0;
        int mDiode1Node1;
        int mDiode2Node0;
        int mDiode2Node1;
        double mDiode1LastVoltDiff;
        double mDiode2LastVoltDiff;

        double[] mLastV = new double[] { 0.0, 0.0, 0.0 };

        public ElmMosfet(bool pnpflag) : base() {
            Pnp = pnpflag ? -1 : 1;
            Hfe = DefaultHfe;
            Vt = DefaultThreshold;
        }
        public ElmMosfet(bool pnpflag, double vt, double hfe) : base() {
            Pnp = pnpflag ? -1 : 1;
            Vt = vt;
            Hfe = hfe;
            AllocNodes();
        }

        public override double VoltageDiff { get { return Volts[IdxD] - Volts[IdxS]; } }

        public override double Power {
            get {
                return Current * (Volts[IdxD] - Volts[IdxS])
                    - DiodeCurrent1 * (Volts[IdxS] - Volts[BodyTerminal])
                    - DiodeCurrent2 * (Volts[IdxD] - Volts[BodyTerminal]);
            }
        }

        public override int PostCount { get { return 3; } }

        public override double GetCurrentIntoNode(int n) {
            if (n == 0) {
                return 0;
            }
            if (n == 3) {
                return -DiodeCurrent1 - DiodeCurrent2;
            }
            if (n == 1) {
                return Current + DiodeCurrent1;
            }
            return -Current + DiodeCurrent2;
        }

        public override void Reset() {
            mLastV[1] = mLastV[2] = 0;
            Volts[IdxG] = Volts[IdxS] = Volts[IdxD] = 0;
            CurCount = 0;
            mDiode1LastVoltDiff = 0.0;
            mDiode2LastVoltDiff = 0.0;
        }

        public override bool AnaGetConnection(int n1, int n2) { return !(n1 == 0 || n2 == 0); }

        public override void AnaStamp() {
            Circuit.StampNonLinear(Nodes[IdxS]);
            Circuit.StampNonLinear(Nodes[IdxD]);

            BodyTerminal = (Pnp == -1) ? IdxD : IdxS;

            if (DoBodyDiode) {
                mDiode1Node0 = Nodes[IdxS];
                mDiode2Node1 = Nodes[IdxD];
                if (Pnp == -1) {
                    /* pnp: diodes conduct when S or D are higher than body */
                    mDiode1Node1 = Nodes[IdxD];
                    mDiode2Node0 = Nodes[IdxD];
                } else {
                    /* npn: diodes conduct when body is higher than S or D */
                    mDiode1Node1 = Nodes[IdxS];
                    mDiode2Node0 = Nodes[IdxS];
                }
                Circuit.RowInfo[mDiode1Node0 - 1].LeftChanges = true;
                Circuit.RowInfo[mDiode1Node1 - 1].LeftChanges = true;
                Circuit.RowInfo[mDiode2Node0 - 1].LeftChanges = true;
                Circuit.RowInfo[mDiode2Node1 - 1].LeftChanges = true;
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
                Current = vds * Gds;
                Gm = 0;
                Mode = 0;
            } else if (vds < vgs - Vt) {
                /* mode: linear */
                Current = Hfe * ((vgs - Vt) * vds - vds * vds * 0.5);
                Gm = Hfe * vds;
                Gds = Hfe * (vgs - vds - Vt);
                Mode = 1;
            } else {
                /* mode: saturation */
                Gm = Hfe * (vgs - Vt);
                /* use very small Gds to avoid nonconvergence */
                Gds = 1e-8;
                Current = 0.5 * Hfe * (vgs - Vt) * (vgs - Vt) + (vds - (vgs - Vt)) * Gds;
                Mode = 2;
            }

            if (DoBodyDiode) {
                var vbs = (Volts[BodyTerminal] - Volts[IdxS]) * Pnp;
                var vbd = (Volts[BodyTerminal] - Volts[IdxD]) * Pnp;
                DiodeDoStep(mDiode1Node0, mDiode1Node1, vbs, ref mDiode1LastVoltDiff);
                DiodeDoStep(mDiode2Node0, mDiode2Node1, vbd, ref mDiode2LastVoltDiff);
                DiodeCurrent1 = (Math.Exp(vbs * DiodeVdCoef) - 1) * DiodeLeakage * Pnp;
                DiodeCurrent2 = (Math.Exp(vbd * DiodeVdCoef) - 1) * DiodeLeakage * Pnp;
            } else {
                DiodeCurrent1 = DiodeCurrent2 = 0;
            }

            /* flip ids if we swapped source and drain above */
            var realIds = Current;
            if (idxS == 2 && Pnp == 1 || idxS == 1 && Pnp == -1) {
                Current = -Current;
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

            Current = 0;
            Gm = 0;
            double Gds = 0;
            if (vgs < Vt) {
                /* should be all zero, but that causes a singular matrix,
                 * so instead we treat it as a large resistor */
                Gds = 1e-8;
                Current = vds * Gds;
                Mode = 0;
            } else if (vds < vgs - Vt) {
                /* linear */
                Current = Hfe * ((vgs - Vt) * vds - vds * vds * 0.5);
                Gm = Hfe * vds;
                Gds = Hfe * (vgs - vds - Vt);
                Mode = 1;
            } else {
                /* saturation; Gds = 0 */
                Gm = Hfe * (vgs - Vt);
                /* use very small Gds to avoid nonconvergence */
                Gds = 1e-8;
                Current = 0.5 * Hfe * (vgs - Vt) * (vgs - Vt) + (vds - (vgs - Vt)) * Gds;
                Mode = 2;
            }

            if (DoBodyDiode) {
                var vbs = (Volts[BodyTerminal] - Volts[IdxS]) * Pnp;
                var vbd = (Volts[BodyTerminal] - Volts[IdxD]) * Pnp;
                DiodeDoStep(mDiode1Node0, mDiode1Node1, vbs, ref mDiode1LastVoltDiff);
                DiodeDoStep(mDiode2Node0, mDiode2Node1, vbd, ref mDiode2LastVoltDiff);
                DiodeCurrent1 = (Math.Exp(vbs * DiodeVdCoef) - 1) * DiodeLeakage * Pnp;
                DiodeCurrent2 = (Math.Exp(vbd * DiodeVdCoef) - 1) * DiodeLeakage * Pnp;
            } else {
                DiodeCurrent1 = DiodeCurrent2 = 0;
            }

            /* flip ids if we swapped source and drain above */
            if (idxS == 2 && Pnp == 1 || idxS == 1 && Pnp == -1) {
                Current = -Current;
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
            if (Math.Abs(Current) > 1e3) {
                Circuit.Stop("Idsが最大を超えました", this);
            }
        }

        static void DiodeDoStep(int n0, int n1, double voltdiff, ref double lastVoltDiff) {
            /* used to have 0.1 here, but needed 0.01 for peak detector */
            if (0.01 < Math.Abs(voltdiff - lastVoltDiff)) {
                Circuit.Converged = false;
            }

            var v_new = voltdiff;
            var v_old = lastVoltDiff;
            /* check new voltage; has current changed by factor of e^2? */
            if (v_new > DiodeVcrit && Math.Abs(v_new - v_old) > (DiodeVscale + DiodeVscale)) {
                if (v_old > 0) {
                    var arg = 1 + (v_new - v_old) / DiodeVscale;
                    if (arg > 0) {
                        /* adjust vnew so that the current is the same
                         * as in linearized model from previous iteration.
                         * current at vnew = old current * arg */
                        v_new = v_old + DiodeVscale * Math.Log(arg);
                    } else {
                        v_new = DiodeVcrit;
                    }
                } else {
                    /* adjust vnew so that the current is the same
                     * as in linearized model from previous iteration.
                     * (1/vscale = slope of load line) */
                    v_new = DiodeVscale * Math.Log(v_new / DiodeVscale);
                }
                Circuit.Converged = false;
            }
            voltdiff = v_new;
            lastVoltDiff = voltdiff;

            /* To prevent a possible singular matrix or other numeric issues, put a tiny conductance
             * in parallel with each P-N junction. */
            var gmin = DiodeLeakage * 0.01;
            if (Circuit.SubIterations > 100) {
                /* if we have trouble converging, put a conductance in parallel with the diode.
                 * Gradually increase the conductance value for each iteration. */
                gmin = Math.Exp(-9 * Math.Log(10) * (1 - Circuit.SubIterations / 3000.0));
                if (0.1 < gmin) {
                    gmin = 0.1;
                }
            }

            /* regular diode or forward-biased zener */
            var eval = Math.Exp(voltdiff * DiodeVdCoef);
            var geq = DiodeVdCoef * DiodeLeakage * eval + gmin;
            var nc = (eval - 1) * DiodeLeakage - geq * voltdiff;

            var row = Circuit.RowInfo[n0 - 1].MapRow;
            var ri = Circuit.RowInfo[n0 - 1];
            if (ri.IsConst) {
                Circuit.RightSide[row] -= geq * ri.Value;
            } else {
                Circuit.Matrix[row, ri.MapCol] += geq;
            }
            row = Circuit.RowInfo[n1 - 1].MapRow;
            ri = Circuit.RowInfo[n1 - 1];
            if (ri.IsConst) {
                Circuit.RightSide[row] -= geq * ri.Value;
            } else {
                Circuit.Matrix[row, ri.MapCol] += geq;
            }
            row = Circuit.RowInfo[n0 - 1].MapRow;
            ri = Circuit.RowInfo[n1 - 1];
            if (ri.IsConst) {
                Circuit.RightSide[row] += geq * ri.Value;
            } else {
                Circuit.Matrix[row, ri.MapCol] -= geq;
            }
            row = Circuit.RowInfo[n1 - 1].MapRow;
            ri = Circuit.RowInfo[n0 - 1];
            if (ri.IsConst) {
                Circuit.RightSide[row] += geq * ri.Value;
            } else {
                Circuit.Matrix[row, ri.MapCol] -= geq;
            }
            Circuit.RightSide[Circuit.RowInfo[n0 - 1].MapRow] -= nc;
            Circuit.RightSide[Circuit.RowInfo[n1 - 1].MapRow] += nc;
        }
    }
}
