using System;

namespace Circuit {
    class Diode {
        /* Electron thermal voltage at SPICE's default temperature of 27 C (300.15 K): */
        const double VT = 0.025865;

        /* The Zener breakdown curve is represented by a steeper exponential, one like the ideal
         * Shockley curve, but flipped and translated. This curve removes the moderating influence
         * of emcoef, replacing vscale and vdcoef with vt and vzcoef.
         * vzcoef is the multiplicative equivalent of dividing by vt (for speed). */
        const double VZ_COEF = 1 / VT;

        int[] mNodes;

        /* The diode's "scale voltage", the voltage increase which will raise current by a factor of e. */
        double mVscale;
        /* The multiplicative equivalent of dividing by vscale (for speed). */
        double mVdCoef;
        /* User-specified diode parameters for Zener voltage. */
        double mZvoltage;

        /* The diode current's scale factor, calculated from the user-specified forward voltage drop. */
        double mLeakage;

        /* Voltage offset for Zener breakdown exponential, calculated from user-specified Zener voltage. */
        double mZoffset;

        /* Critical voltages for limiting the normal diode and Zener breakdown exponentials. */
        double mVcrit;
        double mVzCrit;
        double mLastVoltDiff;

        public Diode() {
            mNodes = new int[2];
        }

        public void Setup(DiodeModel model) {
            mLeakage = model.SaturationCurrent;
            mZvoltage = model.BreakdownVoltage;
            mVscale = model.VScale;
            mVdCoef = model.VdCoef;

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
        }

        public void SetupForDefaultModel() {
            Setup(DiodeModel.GetDefaultModel());
        }

        public void Reset() {
            mLastVoltDiff = 0;
        }

        public void Stamp(int n0, int n1) {
            mNodes[0] = n0;
            mNodes[1] = n1;
            Circuit.StampNonLinear(mNodes[0]);
            Circuit.StampNonLinear(mNodes[1]);
        }

        public void CirDoStep(double voltdiff) {
            /* used to have 0.1 here, but needed 0.01 for peak detector */
            if (0.01 < Math.Abs(voltdiff - mLastVoltDiff)) {
                Circuit.Converged = false;
            }
            voltdiff = limitStep(voltdiff, mLastVoltDiff);
            mLastVoltDiff = voltdiff;

            /* To prevent a possible singular matrix or other numeric issues, put a tiny conductance
             * in parallel with each P-N junction. */
            double gmin = mLeakage * 0.01;
            if (Circuit.SubIterations > 100) {
                /* if we have trouble converging, put a conductance in parallel with the diode.
                 * Gradually increase the conductance value for each iteration. */
                gmin = Math.Exp(-9 * Math.Log(10) * (1 - Circuit.SubIterations / 3000.0));
                if (0.1 < gmin) {
                    gmin = 0.1;
                }
            }

            if (voltdiff >= 0 || mZvoltage == 0) {
                /* regular diode or forward-biased zener */
                double eval = Math.Exp(voltdiff * mVdCoef);
                double geq = mVdCoef * mLeakage * eval + gmin;
                double nc = (eval - 1) * mLeakage - geq * voltdiff;
                Circuit.StampConductance(mNodes[0], mNodes[1], geq);
                Circuit.StampCurrentSource(mNodes[0], mNodes[1], nc);
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

                double geq = mLeakage * (
                    mVdCoef * Math.Exp(voltdiff * mVdCoef)
                    + VZ_COEF * Math.Exp((-voltdiff - mZoffset) * VZ_COEF)
                ) + gmin;

                double nc = mLeakage * (
                    Math.Exp(voltdiff * mVdCoef)
                    - Math.Exp((-voltdiff - mZoffset) * VZ_COEF)
                    - 1
                ) + geq * (-voltdiff);

                Circuit.StampConductance(mNodes[0], mNodes[1], geq);
                Circuit.StampCurrentSource(mNodes[0], mNodes[1], nc);
            }
        }

        public double CirCalculateCurrent(double voltdiff) {
            if (voltdiff >= 0 || mZvoltage == 0) {
                return mLeakage * (Math.Exp(voltdiff * mVdCoef) - 1);
            }
            return mLeakage * (
                Math.Exp(voltdiff * mVdCoef)
                - Math.Exp((-voltdiff - mZoffset) * VZ_COEF)
                - 1
            );
        }

        double limitStep(double vnew, double vold) {
            double arg;
            double oo = vnew;

            /* check new voltage; has current changed by factor of e^2? */
            if (vnew > mVcrit && Math.Abs(vnew - vold) > (mVscale + mVscale)) {
                if (vold > 0) {
                    arg = 1 + (vnew - vold) / mVscale;
                    if (arg > 0) {
                        /* adjust vnew so that the current is the same
                         * as in linearized model from previous iteration.
                         * current at vnew = old current * arg */
                        vnew = vold + mVscale * Math.Log(arg);
                    } else {
                        vnew = mVcrit;
                    }
                } else {
                    /* adjust vnew so that the current is the same
                     * as in linearized model from previous iteration.
                     * (1/vscale = slope of load line) */
                    vnew = mVscale * Math.Log(vnew / mVscale);
                }
                Circuit.Converged = false;
                /*Console.WriteLine(vnew + " " + oo + " " + vold);*/
            } else if (vnew < 0 && mZoffset != 0) {
                /* for Zener breakdown, use the same logic but translate the values,
                 * and replace the normal values with the Zener-specific ones to
                 * account for the steeper exponential of our Zener breakdown curve. */
                vnew = -vnew - mZoffset;
                vold = -vold - mZoffset;

                if (vnew > mVzCrit && Math.Abs(vnew - vold) > (VT + VT)) {
                    if (vold > 0) {
                        arg = 1 + (vnew - vold) / VT;
                        if (arg > 0) {
                            vnew = vold + VT * Math.Log(arg);
                            /*Console.WriteLine(oo + " " + vnew);*/
                        } else {
                            vnew = mVzCrit;
                        }
                    } else {
                        vnew = VT * Math.Log(vnew / VT);
                    }
                    Circuit.Converged = false;
                }
                vnew = -(vnew + mZoffset);
            }
            return vnew;
        }
    }
}
