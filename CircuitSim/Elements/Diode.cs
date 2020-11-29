﻿using System;

namespace Circuit.Elements {
    class Diode {
        CirSim sim;
        Circuit cir;
        int[] nodes;

        /* Electron thermal voltage at SPICE's default temperature of 27 C (300.15 K): */
        const double vt = 0.025865;
        /* The Zener breakdown curve is represented by a steeper exponential, one like the ideal
         * Shockley curve, but flipped and translated. This curve removes the moderating influence
         * of emcoef, replacing vscale and vdcoef with vt and vzcoef.
         * vzcoef is the multiplicative equivalent of dividing by vt (for speed). */
        const double vzcoef = 1 / vt;
        /* The diode's "scale voltage", the voltage increase which will raise current by a factor of e. */
        double vscale;
        /* The multiplicative equivalent of dividing by vscale (for speed). */
        double vdcoef;
        /* User-specified diode parameters for forward voltage drop and Zener voltage. */
        double fwdrop;
        double zvoltage;
        /* The diode current's scale factor, calculated from the user-specified forward voltage drop. */
        double leakage;
        /* Voltage offset for Zener breakdown exponential, calculated from user-specified Zener voltage. */
        double zoffset;
        /* Critical voltages for limiting the normal diode and Zener breakdown exponentials. */
        double vcrit;
        double vzcrit;
        double lastvoltdiff;

        public Diode(CirSim s, Circuit c) {
            sim = s;
            cir = c;
            nodes = new int[2];
        }

        public void setup(DiodeModel model) {
            leakage = model.saturationCurrent;
            zvoltage = model.breakdownVoltage;
            vscale = model.vscale;
            vdcoef = model.vdcoef;

            /* Console.WriteLine("setup " + leakage + " " + zvoltage + " " + model.emissionCoefficient + " " +  vdcoef); */

            /* critical voltage for limiting; current is vscale/sqrt(2) at this voltage */
            vcrit = vscale * Math.Log(vscale / (Math.Sqrt(2) * leakage));
            /* translated, *positive* critical voltage for limiting in Zener breakdown region;
             * limitstep() uses this with translated voltages in an analogous fashion to vcrit. */
            vzcrit = vt * Math.Log(vt / (Math.Sqrt(2) * leakage));
            if (zvoltage == 0) {
                zoffset = 0;
            } else {
                /* calculate offset which will give us 5mA at zvoltage */
                double i = -.005;
                zoffset = zvoltage - Math.Log(-(1 + i / leakage)) / vzcoef;
            }
        }

        public void setupForDefaultModel() {
            setup(DiodeModel.getDefaultModel());
        }

        public void reset() {
            lastvoltdiff = 0;
        }

        double limitStep(double vnew, double vold) {
            double arg;
            double oo = vnew;

            /* check new voltage; has current changed by factor of e^2? */
            if (vnew > vcrit && Math.Abs(vnew - vold) > (vscale + vscale)) {
                if (vold > 0) {
                    arg = 1 + (vnew - vold) / vscale;
                    if (arg > 0) {
                        /* adjust vnew so that the current is the same
                         * as in linearized model from previous iteration.
                         * current at vnew = old current * arg */
                        vnew = vold + vscale * Math.Log(arg);
                    } else {
                        vnew = vcrit;
                    }
                } else {
                    /* adjust vnew so that the current is the same
                     * as in linearized model from previous iteration.
                     * (1/vscale = slope of load line) */
                    vnew = vscale * Math.Log(vnew / vscale);
                }
                cir.Converged = false;
                /*Console.WriteLine(vnew + " " + oo + " " + vold);*/
            } else if (vnew < 0 && zoffset != 0) {
                /* for Zener breakdown, use the same logic but translate the values,
                 * and replace the normal values with the Zener-specific ones to
                 * account for the steeper exponential of our Zener breakdown curve. */
                vnew = -vnew - zoffset;
                vold = -vold - zoffset;

                if (vnew > vzcrit && Math.Abs(vnew - vold) > (vt + vt)) {
                    if (vold > 0) {
                        arg = 1 + (vnew - vold) / vt;
                        if (arg > 0) {
                            vnew = vold + vt * Math.Log(arg);
                            /*Console.WriteLine(oo + " " + vnew);*/
                        } else {
                            vnew = vzcrit;
                        }
                    } else {
                        vnew = vt * Math.Log(vnew / vt);
                    }
                    cir.Converged = false;
                }
                vnew = -(vnew + zoffset);
            }
            return vnew;
        }

        public void stamp(int n0, int n1) {
            nodes[0] = n0;
            nodes[1] = n1;
            cir.stampNonLinear(nodes[0]);
            cir.stampNonLinear(nodes[1]);
        }

        public void doStep(double voltdiff) {
            /* used to have .1 here, but needed .01 for peak detector */
            if (Math.Abs(voltdiff - lastvoltdiff) > .01) {
                cir.Converged = false;
            }
            voltdiff = limitStep(voltdiff, lastvoltdiff);
            lastvoltdiff = voltdiff;

            /* To prevent a possible singular matrix or other numeric issues, put a tiny conductance
             * in parallel with each P-N junction. */
            double gmin = leakage * 0.01;
            if (cir.SubIterations > 100) {
                /* if we have trouble converging, put a conductance in parallel with the diode.
                 * Gradually increase the conductance value for each iteration. */
                gmin = Math.Exp(-9 * Math.Log(10) * (1 - cir.SubIterations / 3000.0));
                if (gmin > .1) {
                    gmin = .1;
                }
            }

            if (voltdiff >= 0 || zvoltage == 0) {
                /* regular diode or forward-biased zener */
                double eval = Math.Exp(voltdiff * vdcoef);
                double geq = vdcoef * leakage * eval + gmin;
                double nc = (eval - 1) * leakage - geq * voltdiff;
                cir.stampConductance(nodes[0], nodes[1], geq);
                cir.stampCurrentSource(nodes[0], nodes[1], nc);
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

                double geq = leakage * (
                    vdcoef * Math.Exp(voltdiff * vdcoef)
                    + vzcoef * Math.Exp((-voltdiff - zoffset) * vzcoef)
                ) + gmin;

                double nc = leakage * (
                    Math.Exp(voltdiff * vdcoef)
                    - Math.Exp((-voltdiff - zoffset) * vzcoef)
                    - 1
                ) + geq * (-voltdiff);

                cir.stampConductance(nodes[0], nodes[1], geq);
                cir.stampCurrentSource(nodes[0], nodes[1], nc);
            }
        }

        public double calculateCurrent(double voltdiff) {
            if (voltdiff >= 0 || zvoltage == 0) {
                return leakage * (Math.Exp(voltdiff * vdcoef) - 1);
            }
            return leakage * (
                Math.Exp(voltdiff * vdcoef)
                - Math.Exp((-voltdiff - zoffset) * vzcoef)
                - 1
            );
        }
    }
}
