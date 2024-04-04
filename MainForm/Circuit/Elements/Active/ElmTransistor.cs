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
		public double Vb { get { return NodeVolts[IdxB]; } }
		public double Vc { get { return NodeVolts[IdxC]; } }
		public double Ve { get { return NodeVolts[IdxE]; } }

		double mFgain;
		double mInv_fgain;
		double mVcrit;
		double mLastVbc;
		double mLastVbe;

		public override int TermCount { get { return 3; } }

		public ElmTransistor() { }

		public ElmTransistor(double vbe, double vbc) {
			mLastVbe = vbe;
			mLastVbc = vbc;
			NodeVolts[IdxB] = 0;
			NodeVolts[IdxC] = -vbe;
			NodeVolts[IdxE] = -vbc;
		}

		public void Setup() {
			mVcrit = VT * Math.Log(VT / (Math.Sqrt(2) * LEAKAGE));
			mFgain = Hfe / (Hfe + 1);
			mInv_fgain = 1 / mFgain;
		}

		public void SetHfe(double hfe) {
			Hfe = hfe;
			Setup();
		}

		public override double GetVoltageDiff() {
			return NodeVolts[IdxC] - NodeVolts[IdxE];
		}

		#region [method(Analyze)]
		public override void Reset() {
			NodeVolts[IdxB] = NodeVolts[IdxC] = NodeVolts[IdxE] = 0;
			mLastVbc = mLastVbe = 0;
		}

		public override void Stamp() {
			StampNonLinear(NodeId[IdxB]);
			StampNonLinear(NodeId[IdxC]);
			StampNonLinear(NodeId[IdxE]);
		}
		#endregion

		#region [method(Circuit)]
		public override void DoIteration() {
			var vbc = NodeVolts[IdxB] - NodeVolts[IdxC]; /* typically negative */
			var vbe = NodeVolts[IdxB] - NodeVolts[IdxE]; /* typically positive */
			if (0.001 < Math.Abs(vbc - mLastVbc) || 0.001 < Math.Abs(vbe - mLastVbe)) {
				/* not converge 0.01 */
				CircuitState.Converged = false;
			}

			/* To prevent a possible singular matrix,
             * put a tiny conductance in parallel with each P-N junction. */
			var g_min = LEAKAGE * 0.01;
			if (CircuitElement.ITER_COUNT > 100) {
				g_min = Math.Exp(-9 * Math.Log(10) * (1 - CircuitElement.ITER_COUNT / 3000.0));
				if (g_min > 0.1) {
					g_min = 0.1;
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
			gcc -= g_min;
			gee -= g_min;

			/***** Set matrix *****/
			var nib = CircuitElement.NODE_INFOS[NodeId[IdxB] - 1];
			var nic = CircuitElement.NODE_INFOS[NodeId[IdxC] - 1];
			var nie = CircuitElement.NODE_INFOS[NodeId[IdxE] - 1];
			var nb = nib.row;
			var nc = nic.row;
			var ne = nie.row;
			if (nib.is_const) {
				CircuitElement.RIGHT_SIDE[nb] += (gee + gec + gce + gcc) * nib.value;
				CircuitElement.RIGHT_SIDE[nc] -= (gce + gcc) * nib.value;
				CircuitElement.RIGHT_SIDE[ne] -= (gee + gec) * nib.value;
			} else {
				CircuitElement.MATRIX[nb, nib.col] -= gee + gec + gce + gcc;
				CircuitElement.MATRIX[nc, nib.col] += gce + gcc;
				CircuitElement.MATRIX[ne, nib.col] += gee + gec;
			}
			if (nic.is_const) {
				CircuitElement.RIGHT_SIDE[nb] -= (gec + gcc) * nic.value;
				CircuitElement.RIGHT_SIDE[nc] += gcc * nic.value;
				CircuitElement.RIGHT_SIDE[ne] += gec * nic.value;
			} else {
				CircuitElement.MATRIX[nb, nic.col] += gec + gcc;
				CircuitElement.MATRIX[nc, nic.col] -= gcc;
				CircuitElement.MATRIX[ne, nic.col] -= gec;
			}
			if (nie.is_const) {
				CircuitElement.RIGHT_SIDE[nb] -= (gee + gce) * nie.value;
				CircuitElement.RIGHT_SIDE[nc] += gce * nie.value;
				CircuitElement.RIGHT_SIDE[ne] += gee * nie.value;
			} else {
				CircuitElement.MATRIX[nb, nie.col] += gee + gce;
				CircuitElement.MATRIX[nc, nie.col] -= gce;
				CircuitElement.MATRIX[ne, nie.col] -= gee;
			}
			/* we are solving for v(k+1), not delta v, so we use formula
             * multiplying J by v(k) */
			CircuitElement.RIGHT_SIDE[nb] += -Ib - (gec + gcc) * vbc - (gee + gce) * vbe;
			CircuitElement.RIGHT_SIDE[nc] += -Ic + gce * vbe + gcc * vbc;
			CircuitElement.RIGHT_SIDE[ne] += -Ie + gee * vbe + gec * vbc;
		}

		public override void FinishIteration() {
			if (Math.Abs(Ic) > 1e12 || Math.Abs(Ib) > 1e12) {
				CircuitState.Stopped = true;
			}
		}

		public override double GetCurrent(int n) {
			if (n == 0) {
				return -Ib;
			}
			if (n == 1) {
				return -Ic;
			}
			return -Ie;
		}
		#endregion

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
				CircuitState.Converged = false;
			}
			return vnew;
		}
	}
}
