using System;

namespace Circuit.Elements.Active {
    class ElmMosfet : BaseElement {
        const int IdxG = 0;
        const int IdxS = 1;
        const int IdxD = 2;

        const double BackwardCompatibilityHfe = 0.02;
        const double DiodeVcrit = 0.6347668814648425;
        const double DiodeVscale = 0.05173;
        const double DiodeLeakage = 1.7143528192808883E-07;
        const double DiodeVdCoef = 19.331142470520007;

        public const double DefaultThreshold = 1.5;

        public static double LastBeta;

        public static double DefaultBeta {
            get { return LastBeta == 0 ? BackwardCompatibilityHfe : LastBeta; }
        }

        public double Vth;
        public double Beta;

        public int Nch { get; private set; }
        public int BodyTerminal { get; private set; }
        public int Mode { get; private set; } = 0;
        public double Gm { get; private set; } = 0;

        public double DiodeCurrent1 { get; private set; }
        public double DiodeCurrent2 { get; private set; }
        public double Vg { get { return Volts[IdxG]; } }
        public double Vs { get { return Volts[IdxS]; } }
        public double Vd { get { return Volts[IdxD]; } }

        int mDiode1Node0;
        int mDiode1Node1;
        int mDiode2Node0;
        int mDiode2Node1;
        double mDiode1LastVoltDiff;
        double mDiode2LastVoltDiff;
        double[] mLastV = new double[] { 0.0, 0.0, 0.0 };

        public ElmMosfet(bool pChFlag) : base() {
            Nch = pChFlag ? -1 : 1;
            Beta = DefaultBeta;
            Vth = DefaultThreshold;
        }
        public ElmMosfet(bool pChFlag, double vth, double beta) : base() {
            Nch = pChFlag ? -1 : 1;
            Vth = vth;
            Beta = beta;
            AllocNodes();
        }

        public override int TermCount { get { return 3; } }

        public override double GetVoltageDiff() { return Volts[IdxD] - Volts[IdxS]; }

        public override void Reset() {
            mLastV[1] = mLastV[2] = 0;
            Volts[IdxG] = Volts[IdxS] = Volts[IdxD] = 0;
            mDiode1LastVoltDiff = 0.0;
            mDiode2LastVoltDiff = 0.0;
        }

        public override bool AnaGetConnection(int n1, int n2) { return !(n1 == 0 || n2 == 0); }

        public override void AnaStamp() {
            Circuit.RowInfo[Nodes[IdxS] - 1].LeftChanges = true;
            Circuit.RowInfo[Nodes[IdxD] - 1].LeftChanges = true;

            BodyTerminal = (Nch == -1) ? IdxD : IdxS;

            mDiode1Node0 = Nodes[IdxS];
            mDiode2Node1 = Nodes[IdxD];
            if (Nch < 0) {
                mDiode1Node1 = Nodes[IdxD];
                mDiode2Node0 = Nodes[IdxD];
            } else {
                mDiode1Node1 = Nodes[IdxS];
                mDiode2Node0 = Nodes[IdxS];
            }
            Circuit.RowInfo[mDiode1Node0 - 1].LeftChanges = true;
            Circuit.RowInfo[mDiode1Node1 - 1].LeftChanges = true;
            Circuit.RowInfo[mDiode2Node0 - 1].LeftChanges = true;
            Circuit.RowInfo[mDiode2Node1 - 1].LeftChanges = true;
        }

        public override double CirGetCurrentIntoNode(int n) {
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

        public override void CirDoIteration() {
            /* ドレインとソースの電圧変化を0.5Vに制限する */
            {
                const double limitDelta = 0.5;
                var vs = Volts[IdxS];
                var vd = Volts[IdxD];
                if (vs > mLastV[IdxS] + limitDelta) {
                    vs = mLastV[IdxS] + limitDelta;
                }
                if (vs < mLastV[IdxS] - limitDelta) {
                    vs = mLastV[IdxS] - limitDelta;
                }
                if (vd > mLastV[IdxD] + limitDelta) {
                    vd = mLastV[IdxD] + limitDelta;
                }
                if (vd < mLastV[IdxD] - limitDelta) {
                    vd = mLastV[IdxD] - limitDelta;
                }
                mLastV[IdxS] = vs;
                mLastV[IdxD] = vd;
                mLastV[IdxG] = Volts[IdxG];
            }

            /* ドレインソース間電圧が負の場合
             * ドレインとソースを入れ替える
             * (電流の計算を単純化するため) */
            int idxS, idxD;
            if (Nch * Volts[IdxD] < Nch * Volts[IdxS]) {
                idxS = IdxD;
                idxD = IdxS;
            } else {
                idxS = IdxS;
                idxD = IdxD;
            }

            double gds;
            var vgs = Volts[IdxG] - Volts[idxS];
            var vds = Volts[idxD] - Volts[idxS];
            {
                var vgs_vth = vgs * Nch - Vth;
                var tmpVds = vds * Nch;
                if (vgs_vth < 0.0) {
                    /* mode: 遮断領域 */
                    /* 電流を0にするべきだが特異な行列となるため
                     * 100MΩとした時の電流にする */
                    gds = 1e-8;
                    Gm = 0;
                    Current = tmpVds * gds;
                    Mode = 0;
                } else if (tmpVds < vgs_vth) {
                    /* mode: 線形領域 */
                    gds = Beta * (vgs_vth - tmpVds);
                    Gm = Beta * tmpVds;
                    Current = Beta * (vgs_vth * tmpVds - tmpVds * tmpVds * 0.5);
                    Mode = 1;
                } else {
                    /* mode: 飽和領域 */
                    gds = 1e-8;
                    Gm = Beta * vgs_vth;
                    Current = 0.5 * Beta * vgs_vth * vgs_vth + (tmpVds - vgs_vth) * gds;
                    Mode = 2;
                }
            }

            /* ドレインソース間電圧が負の場合
             * ドレインとソースを入れ替えているため電流を反転 */
            var realIds = Current;
            if (idxS == 2 && Nch == 1 || idxS == 1 && Nch == -1) {
                Current = -Current;
            }

            /* 還流ダイオード */
            var vbs = (Volts[BodyTerminal] - Volts[IdxS]) * Nch;
            var vbd = (Volts[BodyTerminal] - Volts[IdxD]) * Nch;
            DiodeDoStep(mDiode1Node0, mDiode1Node1, vbs, ref mDiode1LastVoltDiff);
            DiodeDoStep(mDiode2Node0, mDiode2Node1, vbd, ref mDiode2LastVoltDiff);
            DiodeCurrent1 = (Math.Exp(vbs * DiodeVdCoef) - 1) * DiodeLeakage * Nch;
            DiodeCurrent2 = (Math.Exp(vbd * DiodeVdCoef) - 1) * DiodeLeakage * Nch;
            if (BodyTerminal == 1) {
                DiodeCurrent1 = -DiodeCurrent2;
            }
            if (BodyTerminal == 2) {
                DiodeCurrent2 = -DiodeCurrent1;
            }

            var rowD = Circuit.RowInfo[Nodes[idxD] - 1].MapRow;
            var rowS = Circuit.RowInfo[Nodes[idxS] - 1].MapRow;
            var colri = Circuit.RowInfo[Nodes[idxD] - 1];
            if (colri.IsConst) {
                Circuit.RightSide[rowD] -= gds * colri.Value;
                Circuit.RightSide[rowS] += gds * colri.Value;
            } else {
                Circuit.Matrix[rowD, colri.MapCol] += gds;
                Circuit.Matrix[rowS, colri.MapCol] -= gds;
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
                Circuit.RightSide[rowD] += (gds + Gm) * colri.Value;
                Circuit.RightSide[rowS] -= (gds + Gm) * colri.Value;
            } else {
                Circuit.Matrix[rowD, colri.MapCol] -= gds + Gm;
                Circuit.Matrix[rowS, colri.MapCol] += gds + Gm;
            }

            var rs = -Nch * realIds + gds * vds + Gm * vgs;
            rowD = Circuit.RowInfo[Nodes[idxD] - 1].MapRow;
            rowS = Circuit.RowInfo[Nodes[idxS] - 1].MapRow;
            Circuit.RightSide[rowD] += rs;
            Circuit.RightSide[rowS] -= rs;
        }

        static void DiodeDoStep(int n0, int n1, double voltdiff, ref double lastVoltDiff) {
            if (0.001 < Math.Abs(voltdiff - lastVoltDiff)) {
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
