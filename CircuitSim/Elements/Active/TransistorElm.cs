using System;

namespace Circuit.Elements.Active {
    class TransistorElm : BaseElement {
        const int IdxB = 0;
        const int IdxC = 1;
        const int IdxE = 2;

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
        public double Vb { get { return Volts[IdxB]; } }
        public double Vc { get { return Volts[IdxC]; } }
        public double Ve { get { return Volts[IdxE]; } }

        double mFgain;
        double mInv_fgain;
        double mGmin;
        double mVcrit;
        double mLastVbc;
        double mLastVbe;

        public TransistorElm(bool pnpflag) {
            NPN = pnpflag ? -1 : 1;
            Hfe = 100;
        }

        public TransistorElm(StringTokenizer st) {
            NPN = st.nextTokenInt();
            Hfe = 100;
            try {
                mLastVbe = st.nextTokenDouble();
                mLastVbc = st.nextTokenDouble();
                Volts[IdxB] = 0;
                Volts[IdxC] = -mLastVbe;
                Volts[IdxE] = -mLastVbc;
                Hfe = st.nextTokenDouble();
            } catch { }
        }

        public override double Power {
            get { return (Volts[IdxB] - Volts[IdxE]) * Ib + (Volts[IdxC] - Volts[IdxE]) * Ic; }
        }

        public override bool NonLinear { get { return true; } }

        public override int PostCount { get { return 3; } }

        public void SetHfe(double hfe) {
            Hfe = hfe;
            Setup();
        }

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
            Circuit.StampNonLinear(Nodes[IdxB]);
            Circuit.StampNonLinear(Nodes[IdxC]);
            Circuit.StampNonLinear(Nodes[IdxE]);
        }

        public override void CirDoStep() {
            double vbc = Volts[IdxB] - Volts[IdxC]; /* typically negative */
            double vbe = Volts[IdxB] - Volts[IdxE]; /* typically positive */
            if (0.01 < Math.Abs(vbc - mLastVbc) || 0.01 < Math.Abs(vbe - mLastVbe)) {
                /* not converge 0.01 */
                Circuit.Converged = false;
            }

            /* To prevent a possible singular matrix,
             * put a tiny conductance in parallel with each P-N junction. */
            mGmin = LEAKAGE * 0.01;
            if (100 < Circuit.SubIterations) {
                /* if we have trouble converging, put a conductance in parallel with all P-N junctions.
                 * Gradually increase the conductance value for each iteration. */
                mGmin = Math.Exp(-9 * Math.Log(10) * (1 - Circuit.SubIterations / 300.0));
                if (0.1 < mGmin) {
                    mGmin = 0.1;
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
            double expbe = Math.Exp(vbe * pcoef);
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

            Circuit.StampMatrix(Nodes[IdxB], Nodes[IdxB], -gee - gec - gce - gcc);
            Circuit.StampMatrix(Nodes[IdxB], Nodes[IdxC], gec + gcc);
            Circuit.StampMatrix(Nodes[IdxB], Nodes[IdxE], gee + gce);
            Circuit.StampMatrix(Nodes[IdxC], Nodes[IdxB], gce + gcc);
            Circuit.StampMatrix(Nodes[IdxC], Nodes[IdxC], -gcc);
            Circuit.StampMatrix(Nodes[IdxC], Nodes[IdxE], -gce);
            Circuit.StampMatrix(Nodes[IdxE], Nodes[IdxB], gee + gec);
            Circuit.StampMatrix(Nodes[IdxE], Nodes[IdxC], -gec);
            Circuit.StampMatrix(Nodes[IdxE], Nodes[IdxE], -gee);

            /* we are solving for v(k+1), not delta v, so we use formula
             * multiplying J by v(k) */
            Circuit.StampRightSide(Nodes[IdxB], -Ib - (gec + gcc) * vbc - (gee + gce) * vbe);
            Circuit.StampRightSide(Nodes[IdxC], -Ic + gce * vbe + gcc * vbc);
            Circuit.StampRightSide(Nodes[IdxE], -Ie + gee * vbe + gec * vbc);
        }

        public override void CirStepFinished() {
            /* stop for huge currents that make simulator act weird */
            if (Math.Abs(Ic) > 1e12 || Math.Abs(Ib) > 1e12) {
                Circuit.Stop("max current exceeded", this);
            }
        }

        public override void Reset() {
            Volts[IdxB] = Volts[IdxC] = Volts[IdxE] = 0;
            mLastVbc = mLastVbe = 0;
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
                Circuit.Converged = false;
                /*Console.WriteLine(vnew + " " + oo + " " + vold);*/
            }
            return vnew;
        }
    }
}
