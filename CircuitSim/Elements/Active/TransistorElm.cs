using System;

namespace Circuit.Elements.Active {
    class TransistorElm : BaseElement {
        public const int V_B = 0;
        public const int V_C = 1;
        public const int V_E = 2;

        const double VT = 0.025865;
        const double LEAKAGE = 1e-13; /* 1e-6; */
        const double VD_COEF = 1 / VT;
        const double R_GAIN = .5;
        const double INV_R_GAIN = 1 / R_GAIN;

        public double Hfe;

        ///<summary>1 = NPN, -1 = PNP</summary>  
        public int NPN { get; private set; }
        public double Ic { get; private set; }
        public double Ie { get; private set; }
        public double Ib { get; private set; }

        double mFgain;
        double mInv_fgain;
        double mGmin;
        double mVcrit;
        double mLastVbc;
        double mLastVbe;

        public TransistorElm(bool pnpflag) : base() {
            NPN = pnpflag ? -1 : 1;
            Hfe = 100;
        }

        public TransistorElm(StringTokenizer st) : base() {
            NPN = st.nextTokenInt();
            Hfe = 100;
            try {
                mLastVbe = st.nextTokenDouble();
                mLastVbc = st.nextTokenDouble();
                Volts[V_B] = 0;
                Volts[V_C] = -mLastVbe;
                Volts[V_E] = -mLastVbc;
                Hfe = st.nextTokenDouble();
            } catch { }
        }

        public override double Power {
            get { return (Volts[V_B] - Volts[V_E]) * Ib + (Volts[V_C] - Volts[V_E]) * Ic; }
        }

        public override bool NonLinear { get { return true; } }

        public override int PostCount { get { return 3; } }

        public override double GetCurrentIntoNode(int n) {
            if (n == 0) {
                return -Ib;
            }
            if (n == 1) {
                return -Ic;
            }
            return -Ie;
        }

        public override void AnaStamp() {
            mCir.StampNonLinear(Nodes[V_B]);
            mCir.StampNonLinear(Nodes[V_C]);
            mCir.StampNonLinear(Nodes[V_E]);
        }

        public override void CirDoStep() {
            double vbc = Volts[V_B] - Volts[V_C]; /* typically negative */
            double vbe = Volts[V_B] - Volts[V_E]; /* typically positive */
            if (Math.Abs(vbc - mLastVbc) > .01 || /* .01 */
                Math.Abs(vbe - mLastVbe) > .01) {
                mCir.Converged = false;
            }
            /* To prevent a possible singular matrix,
             * put a tiny conductance in parallel with each P-N junction. */
            mGmin = LEAKAGE * 0.01;
            if (mCir.SubIterations > 100) {
                /* if we have trouble converging, put a conductance in parallel with all P-N junctions.
                 * Gradually increase the conductance value for each iteration. */
                mGmin = Math.Exp(-9 * Math.Log(10) * (1 - mCir.SubIterations / 300.0));
                if (mGmin > .1) {
                    mGmin = .1;
                }
                /*Console.WriteLine("gmin " + gmin + " vbc " + vbc + " vbe " + vbe); */
            }

            /*Console.WriteLine("T " + vbc + " " + vbe + "\n"); */
            vbc = NPN * limitStep(NPN * vbc, NPN * mLastVbc);
            vbe = NPN * limitStep(NPN * vbe, NPN * mLastVbe);
            mLastVbc = vbc;
            mLastVbe = vbe;
            double pcoef = VD_COEF * NPN;
            double expbc = Math.Exp(vbc * pcoef);
            /*if (expbc > 1e13 || Double.isInfinite(expbc))
             * expbc = 1e13;*/
            double expbe = Math.Exp(vbe * pcoef);
            /*if (expbe > 1e13 || Double.isInfinite(expbe))
             * expbe = 1e13;*/
            Ie = NPN * LEAKAGE * (-mInv_fgain * (expbe - 1) + (expbc - 1));
            Ic = NPN * LEAKAGE * ((expbe - 1) - INV_R_GAIN * (expbc - 1));
            Ib = -(Ie + Ic);
            /*Console.WriteLine("gain " + ic/ib);
            Console.WriteLine("T " + vbc + " " + vbe + " " + ie + " " + ic + "\n"); */
            double gee = -LEAKAGE * VD_COEF * expbe * mInv_fgain;
            double gec = LEAKAGE * VD_COEF * expbc;
            double gce = -gee * mFgain;
            double gcc = -gec * INV_R_GAIN;

            /* add minimum conductance (gmin) between b,e and b,c */
            gcc -= mGmin;
            gee -= mGmin;

            /* stamps from page 302 of Pillage.
             * node 0 is the base,
             * node 1 the collector,
             * node 2 the emitter. */
            mCir.StampMatrix(Nodes[V_B], Nodes[V_B], -gee - gec - gce - gcc);
            mCir.StampMatrix(Nodes[V_B], Nodes[V_C], gec + gcc);
            mCir.StampMatrix(Nodes[V_B], Nodes[V_E], gee + gce);
            mCir.StampMatrix(Nodes[V_C], Nodes[V_B], gce + gcc);
            mCir.StampMatrix(Nodes[V_C], Nodes[V_C], -gcc);
            mCir.StampMatrix(Nodes[V_C], Nodes[V_E], -gce);
            mCir.StampMatrix(Nodes[V_E], Nodes[V_B], gee + gec);
            mCir.StampMatrix(Nodes[V_E], Nodes[V_C], -gec);
            mCir.StampMatrix(Nodes[V_E], Nodes[V_E], -gee);

            /* we are solving for v(k+1), not delta v, so we use formula
             * 10.5.13 (from Pillage), multiplying J by v(k) */

            mCir.StampRightSide(Nodes[V_B], -Ib - (gec + gcc) * vbc - (gee + gce) * vbe);
            mCir.StampRightSide(Nodes[V_C], -Ic + gce * vbe + gcc * vbc);
            mCir.StampRightSide(Nodes[V_E], -Ie + gee * vbe + gec * vbc);
        }

        public override void CirStepFinished() {
            /* stop for huge currents that make simulator act weird */
            if (Math.Abs(Ic) > 1e12 || Math.Abs(Ib) > 1e12) {
                mCir.Stop("max current exceeded", null);
            }
        }

        public override void Reset() {
            Volts[V_B] = Volts[V_C] = Volts[V_E] = 0;
            mLastVbc = mLastVbe = 0;
        }

        public override double GetScopeValue(Scope.VAL x) {
            switch (x) {
            case Scope.VAL.VBE:
                return Volts[V_B] - Volts[V_E];
            case Scope.VAL.VBC:
                return Volts[V_B] - Volts[V_C];
            case Scope.VAL.VCE:
                return Volts[V_C] - Volts[V_E];
            }
            return 0;
        }

        public void Setup() {
            mVcrit = VT * Math.Log(VT / (Math.Sqrt(2) * LEAKAGE));
            mFgain = Hfe / (Hfe + 1);
            mInv_fgain = 1 / mFgain;
        }

        double limitStep(double vnew, double vold) {
            double arg;
            double oo = vnew;

            if (vnew > mVcrit && Math.Abs(vnew - vold) > (VT + VT)) {
                if (vold > 0) {
                    arg = 1 + (vnew - vold) / VT;
                    if (arg > 0) {
                        vnew = vold + VT * Math.Log(arg);
                    } else {
                        vnew = mVcrit;
                    }
                } else {
                    vnew = VT * Math.Log(vnew / VT);
                }
                mCir.Converged = false;
                /*Console.WriteLine(vnew + " " + oo + " " + vold);*/
            }
            return vnew;
        }
    }
}
