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

		///<summary>NPN=1, PNP=-1</summary>  
		public int NPN;
		public double Hfe;

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

		public ElmTransistor() { }

		public ElmTransistor(double vbe, double vbc) {
			mLastVbe = vbe;
			mLastVbc = vbc;
			Volts[IdxB] = 0;
			Volts[IdxC] = -vbe;
			Volts[IdxE] = -vbc;
		}

		public override int TermCount { get { return 3; } }

		public override double VoltageDiff { get { return Volts[IdxC] - Volts[IdxE]; } }

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

		public override void Stamp() {
			CircuitElement.RowInfo[Nodes[IdxB] - 1].LeftChanges = true;
			CircuitElement.RowInfo[Nodes[IdxC] - 1].LeftChanges = true;
			CircuitElement.RowInfo[Nodes[IdxE] - 1].LeftChanges = true;
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

		public override void DoIteration() {
			var vbc = Volts[IdxB] - Volts[IdxC]; /* typically negative */
			var vbe = Volts[IdxB] - Volts[IdxE]; /* typically positive */
			if (0.001 < Math.Abs(vbc - mLastVbc) || 0.001 < Math.Abs(vbe - mLastVbe)) {
				/* not converge 0.01 */
				CircuitElement.Converged = false;
			}

			/* To prevent a possible singular matrix,
             * put a tiny conductance in parallel with each P-N junction. */
			mGmin = LEAKAGE * 0.01;
			if (100 < CircuitElement.SubIterations) {
				/* if we have trouble converging, put a conductance in parallel with all P-N junctions.
                 * Gradually increase the conductance value for each iteration. */
				mGmin = Math.Exp(-9 * Math.Log(10) * (1 - CircuitElement.SubIterations / 300.0));
				if (0.1 < mGmin) {
					mGmin = 0.1;
				}
			}

			vbc = NPN * limitStep(NPN * vbc, NPN * mLastVbc);
			vbe = NPN * limitStep(NPN * vbe, NPN * mLastVbe);
			mLastVbc = vbc;
			mLastVbe = vbe;
			var pcoef = VD_COEF * NPN;
			var expbc = Math.Exp(vbc * pcoef);
			var expbe = Math.Exp(vbe * pcoef);
			Ie = NPN * LEAKAGE * (-mInv_fgain * (expbe - 1) + (expbc - 1));
			Ic = NPN * LEAKAGE * ((expbe - 1) - INV_R_GAIN * (expbc - 1));
			Ib = -(Ie + Ic);

			var gee = -LEAKAGE * VD_COEF * expbe * mInv_fgain;
			var gec = LEAKAGE * VD_COEF * expbc;
			var gce = -gee * mFgain;
			var gcc = -gec * INV_R_GAIN;

			/* add minimum conductance (gmin) between b,e and b,c */
			gcc -= mGmin;
			gee -= mGmin;

			var rowB = CircuitElement.RowInfo[Nodes[IdxB] - 1].MapRow;
			var rowC = CircuitElement.RowInfo[Nodes[IdxC] - 1].MapRow;
			var rowE = CircuitElement.RowInfo[Nodes[IdxE] - 1].MapRow;
			var colri = CircuitElement.RowInfo[Nodes[IdxB] - 1];
			if (colri.IsConst) {
				CircuitElement.RightSide[rowB] += (gee + gec + gce + gcc) * colri.Value;
				CircuitElement.RightSide[rowC] -= (gce + gcc) * colri.Value;
				CircuitElement.RightSide[rowE] -= (gee + gec) * colri.Value;
			} else {
				CircuitElement.Matrix[rowB, colri.MapCol] -= gee + gec + gce + gcc;
				CircuitElement.Matrix[rowC, colri.MapCol] += gce + gcc;
				CircuitElement.Matrix[rowE, colri.MapCol] += gee + gec;
			}
			colri = CircuitElement.RowInfo[Nodes[IdxC] - 1];
			if (colri.IsConst) {
				CircuitElement.RightSide[rowB] -= (gec + gcc) * colri.Value;
				CircuitElement.RightSide[rowC] += gcc * colri.Value;
				CircuitElement.RightSide[rowE] += gec * colri.Value;
			} else {
				CircuitElement.Matrix[rowB, colri.MapCol] += gec + gcc;
				CircuitElement.Matrix[rowC, colri.MapCol] -= gcc;
				CircuitElement.Matrix[rowE, colri.MapCol] -= gec;
			}
			colri = CircuitElement.RowInfo[Nodes[IdxE] - 1];
			if (colri.IsConst) {
				CircuitElement.RightSide[rowB] -= (gee + gce) * colri.Value;
				CircuitElement.RightSide[rowC] += gce * colri.Value;
				CircuitElement.RightSide[rowE] += gee * colri.Value;
			} else {
				CircuitElement.Matrix[rowB, colri.MapCol] += gee + gce;
				CircuitElement.Matrix[rowC, colri.MapCol] -= gce;
				CircuitElement.Matrix[rowE, colri.MapCol] -= gee;
			}

			/* we are solving for v(k+1), not delta v, so we use formula
             * multiplying J by v(k) */
			rowB = CircuitElement.RowInfo[Nodes[IdxB] - 1].MapRow;
			rowC = CircuitElement.RowInfo[Nodes[IdxC] - 1].MapRow;
			rowE = CircuitElement.RowInfo[Nodes[IdxE] - 1].MapRow;
			CircuitElement.RightSide[rowB] += -Ib - (gec + gcc) * vbc - (gee + gce) * vbe;
			CircuitElement.RightSide[rowC] += -Ic + gce * vbe + gcc * vbc;
			CircuitElement.RightSide[rowE] += -Ie + gee * vbe + gec * vbc;
		}

		public override void IterationFinished() {
			if (Math.Abs(Ic) > 1e12 || Math.Abs(Ib) > 1e12) {
				CircuitElement.Stop(this);
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
				CircuitElement.Converged = false;
			}
			return vnew;
		}
	}
}
