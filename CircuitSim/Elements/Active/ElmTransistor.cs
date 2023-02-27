using System;
using System.Drawing;

namespace Circuit.Elements.Active {
    class ElmTransistor : BaseElement {
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

        public Point[] Coll;
        public Point[] Emit;

        double mFgain;
        double mInv_fgain;
        double mGmin;
        double mVcrit;
        double mLastVbc;
        double mLastVbe;

        public ElmTransistor(bool pnpflag) {
            NPN = pnpflag ? -1 : 1;
            Hfe = 100;
        }

        public ElmTransistor(int npn, double hfe, double vbe, double vbc) {
            NPN = npn;
            Hfe = hfe;
            mLastVbe = vbe;
            mLastVbc = vbc;
            Volts[IdxB] = 0;
            Volts[IdxC] = -vbe;
            Volts[IdxE] = -vbc;
        }

        public override double Power {
            get { return (Volts[IdxB] - Volts[IdxE]) * Ib + (Volts[IdxC] - Volts[IdxE]) * Ic; }
        }

        public override double VoltageDiff { get { return Volts[IdxC] - Volts[IdxE]; } }

        public override int PostCount { get { return 3; } }

        public override Point GetPost(int n) {
            return (n == 0) ? Post1 : (n == 1) ? Coll[0] : Emit[0];
        }

        public override double CirGetCurrentIntoNode(int n) {
            if (n == 0) {
                return -Ib;
            }
            if (n == 1) {
                return -Ic;
            }
            return -Ie;
        }

        public void SetHfe(double hfe) {
            Hfe = hfe;
            Setup();
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

        public override void AnaStamp() {
            Circuit.RowInfo[Nodes[IdxB] - 1].LeftChanges = true;
            Circuit.RowInfo[Nodes[IdxC] - 1].LeftChanges = true;
            Circuit.RowInfo[Nodes[IdxE] - 1].LeftChanges = true;
        }

        public override void CirDoIteration() {
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
            }

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

            double gee = -LEAKAGE * VD_COEF * expbe * mInv_fgain;
            double gec = LEAKAGE * VD_COEF * expbc;
            double gce = -gee * mFgain;
            double gcc = -gec * INV_R_GAIN;

            /* add minimum conductance (gmin) between b,e and b,c */
            gcc -= mGmin;
            gee -= mGmin;

            var rowB = Circuit.RowInfo[Nodes[IdxB] - 1].MapRow;
            var rowC = Circuit.RowInfo[Nodes[IdxC] - 1].MapRow;
            var rowE = Circuit.RowInfo[Nodes[IdxE] - 1].MapRow;
            var colri = Circuit.RowInfo[Nodes[IdxB] - 1];
            if (colri.IsConst) {
                Circuit.RightSide[rowB] += (gee + gec + gce + gcc) * colri.Value;
                Circuit.RightSide[rowC] -= (gce + gcc) * colri.Value;
                Circuit.RightSide[rowE] -= (gee + gec) * colri.Value;
            } else {
                Circuit.Matrix[rowB, colri.MapCol] -= gee + gec + gce + gcc;
                Circuit.Matrix[rowC, colri.MapCol] += gce + gcc;
                Circuit.Matrix[rowE, colri.MapCol] += gee + gec;
            }
            colri = Circuit.RowInfo[Nodes[IdxC] - 1];
            if (colri.IsConst) {
                Circuit.RightSide[rowB] -= (gec + gcc) * colri.Value;
                Circuit.RightSide[rowC] += gcc * colri.Value;
                Circuit.RightSide[rowE] += gec * colri.Value;
            } else {
                Circuit.Matrix[rowB, colri.MapCol] += gec + gcc;
                Circuit.Matrix[rowC, colri.MapCol] -= gcc;
                Circuit.Matrix[rowE, colri.MapCol] -= gec;
            }
            colri = Circuit.RowInfo[Nodes[IdxE] - 1];
            if (colri.IsConst) {
                Circuit.RightSide[rowB] -= (gee + gce) * colri.Value;
                Circuit.RightSide[rowC] += gce * colri.Value;
                Circuit.RightSide[rowE] += gee * colri.Value;
            } else {
                Circuit.Matrix[rowB, colri.MapCol] += gee + gce;
                Circuit.Matrix[rowC, colri.MapCol] -= gce;
                Circuit.Matrix[rowE, colri.MapCol] -= gee;
            }

            /* we are solving for v(k+1), not delta v, so we use formula
             * multiplying J by v(k) */
            rowB = Circuit.RowInfo[Nodes[IdxB] - 1].MapRow;
            rowC = Circuit.RowInfo[Nodes[IdxC] - 1].MapRow;
            rowE = Circuit.RowInfo[Nodes[IdxE] - 1].MapRow;
            Circuit.RightSide[rowB] += -Ib - (gec + gcc) * vbc - (gee + gce) * vbe;
            Circuit.RightSide[rowC] += -Ic + gce * vbe + gcc * vbc;
            Circuit.RightSide[rowE] += -Ie + gee * vbe + gec * vbc;
        }

        public override void CirIterationFinished() {
            if (Math.Abs(Ic) > 1e12) {
                Circuit.Stop("Icが最大電流を超えました", this);
            }
            if (Math.Abs(Ib) > 1e12) {
                Circuit.Stop("Ibが最大電流を超えました", this);
            }
        }

        double limitStep(double vnew, double vold) {
            double arg;
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
            }
            return vnew;
        }
    }
}
