using System;

namespace Circuit.Elements.Active {
    class ElmDiode : BaseElement {
        public static string lastModelName = "default";

        public string mModelName;
        public DiodeModel mModel;

        /* Electron thermal voltage at SPICE's default temperature of 27 C (300.15 K): */
        const double VT = 0.025865;

        /* The Zener breakdown curve is represented by a steeper exponential, one like the ideal
         * Shockley curve, but flipped and translated. This curve removes the moderating influence
         * of emcoef, replacing vscale and vdcoef with vt and vzcoef.
         * vzcoef is the multiplicative equivalent of dividing by vt (for speed). */
        const double VZ_COEF = 1 / VT;

        bool mHasResistance;
        int mNodes0;
        int mNodes1;
        int mDiodeEndNode;
        double mLastVoltDiff;

        /// <summary>
        /// The diode's "scale voltage", the voltage increase which will raise current by a factor of e.
        /// </summary>
        double mVscale;

        /// <summary>
        /// The multiplicative equivalent of dividing by vscale (for speed).
        /// </summary>
        double mVdCoef;

        /// <summary>
        /// The diode current's scale factor, calculated from the user-specified forward voltage drop.
        /// </summary>
        double mLeakage;

        /// <summary>
        /// Voltage offset for Zener breakdown exponential, calculated from user-specified Zener voltage.
        /// </summary>
        double mZoffset;

        /// <summary>
        /// User-specified diode parameters for Zener voltage.
        /// </summary>
        double mZvoltage;

        /// <summary>
        /// Critical voltages for limiting the normal diode.
        /// </summary>
        double mVcrit;

        /// <summary>
        /// Critical voltages for limiting Zener breakdown exponentials.
        /// </summary>
        double mVzCrit;

        public ElmDiode() : base() {
            mModelName = lastModelName;
            Setup();
        }

        public ElmDiode(string modelName) : base() {
            mModelName = modelName;
            Setup();
        }

        public ElmDiode(StringTokenizer st, bool forwardDrop = false, bool model = false) : base() {
            const double defaultdrop = 0.805904783;
            double fwdrop = defaultdrop;
            double zvoltage = 0;
            if (model) {
                if (st.nextToken(out mModelName, mModelName)) {
                    mModelName = Utils.Unescape(mModelName);
                }
            } else {
                if (forwardDrop) {
                    fwdrop = st.nextTokenDouble();
                }
                mModel = DiodeModel.GetModelWithParameters(fwdrop, zvoltage);
                mModelName = mModel.Name;
            }
            Setup();
        }

        public override int TermCount { get { return 2; } }

        public override int AnaInternalNodeCount { get { return mHasResistance ? 1 : 0; } }

        public void Setup() {
            mModel = DiodeModel.GetModelWithNameOrCopy(mModelName, mModel);
            mModelName = mModel.Name;
            mLeakage = mModel.SaturationCurrent;
            mZvoltage = mModel.BreakdownVoltage;
            mVscale = mModel.VScale;
            mVdCoef = mModel.VdCoef;

            /* critical voltage for limiting; current is vscale/sqrt(2) at this voltage */
            mVcrit = mVscale * Math.Log(mVscale / (Math.Sqrt(2) * mLeakage));
            /* translated, *positive* critical voltage for limiting in Zener breakdown region;
             * limitstep() uses this with translated voltages in an analogous fashion to vcrit. */
            mVzCrit = VT * Math.Log(VT / (Math.Sqrt(2) * mLeakage));
            if (mZvoltage == 0) {
                mZoffset = 0;
            } else {
                /* calculate offset which will give us 5mA at zvoltage */
                double i = -0.005;
                mZoffset = mZvoltage - Math.Log(-(1 + i / mLeakage)) / VZ_COEF;
            }

            mHasResistance = 0 < mModel.SeriesResistance;
            mDiodeEndNode = mHasResistance ? 2 : 1;
            AllocNodes();
        }

        public override void Reset() {
            mLastVoltDiff = 0;
            Volts[0] = Volts[1] = 0;
            if (mHasResistance) {
                Volts[2] = 0;
            }
        }

        public override void AnaStamp() {
            if (mHasResistance) {
                /* create diode from node 0 to internal node */
                stamp(Nodes[0], Nodes[2]);
                /* create resistor from internal node to node 1 */
                var r0 = 1.0 / mModel.SeriesResistance;
                Circuit.Matrix[Nodes[1] - 1, Nodes[1] - 1] += r0;
                Circuit.Matrix[Nodes[2] - 1, Nodes[2] - 1] += r0;
                Circuit.Matrix[Nodes[1] - 1, Nodes[2] - 1] -= r0;
                Circuit.Matrix[Nodes[2] - 1, Nodes[1] - 1] -= r0;
            } else {
                /* don't need any internal nodes if no series resistance */
                stamp(Nodes[0], Nodes[1]);
            }
        }

        public override void CirDoIteration() {
            var voltdiff = Volts[0] - Volts[mDiodeEndNode];
            if (0.001 < Math.Abs(voltdiff - mLastVoltDiff)) {
                Circuit.Converged = false;
            }

            {
                var v_new = voltdiff;
                var v_old = mLastVoltDiff;
                /* check new voltage; has current changed by factor of e^2? */
                if (v_new > mVcrit && Math.Abs(v_new - v_old) > (mVscale + mVscale)) {
                    if (v_old > 0) {
                        var arg = 1 + (v_new - v_old) / mVscale;
                        if (arg > 0) {
                            /* adjust vnew so that the current is the same
                             * as in linearized model from previous iteration.
                             * current at vnew = old current * arg */
                            v_new = v_old + mVscale * Math.Log(arg);
                        } else {
                            v_new = mVcrit;
                        }
                    } else {
                        /* adjust vnew so that the current is the same
                         * as in linearized model from previous iteration.
                         * (1/vscale = slope of load line) */
                        v_new = mVscale * Math.Log(v_new / mVscale);
                    }
                    Circuit.Converged = false;
                } else if (v_new < 0 && mZoffset != 0) {
                    /* for Zener breakdown, use the same logic but translate the values,
                     * and replace the normal values with the Zener-specific ones to
                     * account for the steeper exponential of our Zener breakdown curve. */
                    v_new = -v_new - mZoffset;
                    v_old = -v_old - mZoffset;
                    if (v_new > mVzCrit && Math.Abs(v_new - v_old) > (VT + VT)) {
                        if (v_old > 0) {
                            var arg = 1 + (v_new - v_old) / VT;
                            if (arg > 0) {
                                v_new = v_old + VT * Math.Log(arg);
                            } else {
                                v_new = mVzCrit;
                            }
                        } else {
                            v_new = VT * Math.Log(v_new / VT);
                        }
                        Circuit.Converged = false;
                    }
                    v_new = -(v_new + mZoffset);
                }
                voltdiff = v_new;
                mLastVoltDiff = voltdiff;
            }

            {
                /* To prevent a possible singular matrix or other numeric issues, put a tiny conductance
                 * in parallel with each P-N junction. */
                var gmin = mLeakage * 0.01;
                if (Circuit.SubIterations > 100) {
                    /* if we have trouble converging, put a conductance in parallel with the diode.
                     * Gradually increase the conductance value for each iteration. */
                    gmin = Math.Exp(-9 * Math.Log(10) * (1 - Circuit.SubIterations / 3000.0));
                    if (0.1 < gmin) {
                        gmin = 0.1;
                    }
                }
                double geq;
                double nc;
                if (voltdiff >= 0 || mZvoltage == 0) {
                    /* regular diode or forward-biased zener */
                    var eval = Math.Exp(voltdiff * mVdCoef);
                    geq = mVdCoef * mLeakage * eval + gmin;
                    nc = (eval - 1) * mLeakage - geq * voltdiff;
                } else {
                    /* Zener diode */
                    /* For reverse-biased Zener diodes, mimic the Zener breakdown curve with an
                     * exponential similar to the ideal Shockley curve. (The real breakdown curve
                     * isn't a simple exponential, but this approximation should be OK.) */
                    /* 
                     * I(Vd) = Is * (exp[Vd*C] - exp[(-Vd-Vz)*Cz] - 1 )
                     *
                     * geq is I'(Vd)
                     * nc is I(Vd) + I'(Vd)*(-Vd)
                     */
                    geq = mLeakage * (
                        mVdCoef * Math.Exp(voltdiff * mVdCoef)
                        + VZ_COEF * Math.Exp((-voltdiff - mZoffset) * VZ_COEF)
                    ) + gmin;
                    nc = mLeakage * (
                        Math.Exp(voltdiff * mVdCoef)
                        - Math.Exp((-voltdiff - mZoffset) * VZ_COEF)
                        - 1
                    ) + geq * (-voltdiff);
                }
                var row = Circuit.RowInfo[mNodes0 - 1].MapRow;
                var ri = Circuit.RowInfo[mNodes0 - 1];
                if (ri.IsConst) {
                    Circuit.RightSide[row] -= geq * ri.Value;
                } else {
                    Circuit.Matrix[row, ri.MapCol] += geq;
                }
                row = Circuit.RowInfo[mNodes1 - 1].MapRow;
                ri = Circuit.RowInfo[mNodes1 - 1];
                if (ri.IsConst) {
                    Circuit.RightSide[row] -= geq * ri.Value;
                } else {
                    Circuit.Matrix[row, ri.MapCol] += geq;
                }
                row = Circuit.RowInfo[mNodes0 - 1].MapRow;
                ri = Circuit.RowInfo[mNodes1 - 1];
                if (ri.IsConst) {
                    Circuit.RightSide[row] += geq * ri.Value;
                } else {
                    Circuit.Matrix[row, ri.MapCol] -= geq;
                }
                row = Circuit.RowInfo[mNodes1 - 1].MapRow;
                ri = Circuit.RowInfo[mNodes0 - 1];
                if (ri.IsConst) {
                    Circuit.RightSide[row] += geq * ri.Value;
                } else {
                    Circuit.Matrix[row, ri.MapCol] -= geq;
                }
                Circuit.RightSide[Circuit.RowInfo[mNodes0 - 1].MapRow] -= nc;
                Circuit.RightSide[Circuit.RowInfo[mNodes1 - 1].MapRow] += nc;
            }
        }

        public override void CirSetVoltage(int n, double c) {
            Volts[n] = c;
            var voltdiff = Volts[0] - Volts[mDiodeEndNode];
            if (voltdiff >= 0 || mZvoltage == 0) {
                Current = mLeakage * (Math.Exp(voltdiff * mVdCoef) - 1);
            } else {
                Current = mLeakage * (
                    Math.Exp(voltdiff * mVdCoef)
                    - Math.Exp((-voltdiff - mZoffset) * VZ_COEF)
                    - 1
                );
            }
        }

        public override void CirIterationFinished() {
            if (Math.Abs(Current) > 1e12) {
                Circuit.Stop("最大電流を超えました", this);
            }
        }

        void stamp(int n0, int n1) {
            mNodes0 = n0;
            mNodes1 = n1;
            Circuit.RowInfo[mNodes0 - 1].LeftChanges = true;
            Circuit.RowInfo[mNodes1 - 1].LeftChanges = true;
        }
    }
}
