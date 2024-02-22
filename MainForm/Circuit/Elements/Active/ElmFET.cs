namespace Circuit.Elements.Active {
	class ElmFET : BaseElement {
		protected const int IdxG = 0;
		protected const int IdxS = 1;
		protected const int IdxD = 2;

		const double DiodeVScale = 0.05173;
		const double DiodeVdCoef = 19.331142470520007;
		const double DiodeLeakage = 1.7143528192808883E-07;
		const double DiodeVCrit = 6.34767e-01; //DiodeVScale * Math.Log(DiodeVScale / (Math.Sqrt(2) * DiodeLeakage));

		public int Nch;
		public bool MOS;
		public double Vth;
		public double Beta;

		public int Mode;
		public double Gm;
		public double DiodeCurrent1;
		public double DiodeCurrent2;

		public double Vg { get { return Volts[IdxG]; } }
		public double Vs { get { return Volts[IdxS]; } }
		public double Vd { get { return Volts[IdxD]; } }

		double mTempVg = 0.0;
		double mTempVs = 0.0;
		double mTempVd = 0.0;
		double mLastVg = 0.0;
		double mLastVs = 0.0;
		double mLastVd = 0.0;

		int mBodyTerminal;
		int mDiodeNodes1A;
		int mDiodeNodes1B;
		int mDiodeNodes2A;
		int mDiodeNodes2B;
		double mDiodeLastVdiff1 = 0.0;
		double mDiodeLastVdiff2 = 0.0;

		public override int TermCount { get { return 3; } }

		public override double VoltageDiff { get { return Volts[IdxG] - Volts[IdxS]; } }

		public override bool GetConnection(int n1, int n2) { return !(n1 == 0 || n2 == 0); }

		public override void Reset() {
			Volts[IdxG] = Volts[IdxS] = Volts[IdxD] = 0;
			mLastVs = 0.0;
			mLastVd = 0.0;
			mLastVg = 0.0;
			mDiodeLastVdiff1 = 0;
			mDiodeLastVdiff2 = 0;
			DiodeCurrent1 = 0.0;
			DiodeCurrent2 = 0.0;
		}

		public override void Stamp() {
			CircuitElement.StampNonLinear(Nodes[IdxS]);
			CircuitElement.StampNonLinear(Nodes[IdxD]);
			mBodyTerminal = (Nch < 0) ? IdxD : IdxS;
			if (MOS) {
				if (Nch < 0) {
					mDiodeNodes1A = Nodes[IdxS];
					mDiodeNodes1B = Nodes[mBodyTerminal];
					mDiodeNodes2A = Nodes[IdxD];
					mDiodeNodes2B = Nodes[mBodyTerminal];
				} else {
					mDiodeNodes1A = Nodes[mBodyTerminal];
					mDiodeNodes1B = Nodes[IdxS];
					mDiodeNodes2A = Nodes[mBodyTerminal];
					mDiodeNodes2B = Nodes[IdxD];
				}
				CircuitElement.StampNonLinear(mDiodeNodes1A);
				CircuitElement.StampNonLinear(mDiodeNodes1B);
				CircuitElement.StampNonLinear(mDiodeNodes2A);
				CircuitElement.StampNonLinear(mDiodeNodes2B);
			}
		}

		public override double GetCurrentIntoNode(int n) {
			if (n == 0) {
				return 0;
			}
			if (n == 1) {
				return Current + DiodeCurrent1;
			}
			return -Current + DiodeCurrent2;
		}

		public override void DoIteration() {
			Calc(false);
		}

		public override void FinishIteration() {
			Calc(true);
			if (mBodyTerminal == IdxS) {
				DiodeCurrent1 = -DiodeCurrent2;
			}
			if (mBodyTerminal == IdxD) {
				DiodeCurrent2 = -DiodeCurrent1;
			}
		}

		protected static void DiodeDoIteration(double vnew, ref double vold, int nodeA, int nodeB) {
			if (Math.Abs(vnew - vold) > 0.01) {
				CircuitElement.Converged = false;
			}
			if (vnew > DiodeVCrit && Math.Abs(vnew - vold) > (DiodeVScale + DiodeVScale)) {
				if (vold > 0) {
					var arg = 1 + (vnew - vold) / DiodeVScale;
					if (arg > 0) {
						vnew = vold + DiodeVScale * Math.Log(arg);
					} else {
						vnew = DiodeVCrit;
					}
				} else {
					vnew = DiodeVScale * Math.Log(vnew / DiodeVScale);
				}
				CircuitElement.Converged = false;
			}
			vold = vnew;
			var gmin = DiodeLeakage * 0.01;
			if (CircuitElement.SubIterations > 100) {
				gmin = Math.Exp(-9 * Math.Log(10) * (1 - CircuitElement.SubIterations / 3000.0));
				if (gmin > 0.1) {
					gmin = 0.1;
				}
			}
			var eval = Math.Exp(vnew * DiodeVdCoef);
			var geq = DiodeVdCoef * DiodeLeakage * eval + gmin;
			var nc = (eval - 1) * DiodeLeakage - geq * vnew;
			CircuitElement.StampConductance(nodeA, nodeB, geq);
			CircuitElement.StampCurrentSource(nodeA, nodeB, nc);
		}

		protected static double DiodeCalculateCurrent(double voltdiff) {
			return DiodeLeakage * (Math.Exp(voltdiff * DiodeVdCoef) - 1);
		}

		void Calc(bool finished) {
			mTempVg = Volts[IdxG];
			mTempVs = Volts[IdxS];
			mTempVd = Volts[IdxD];
			if (!finished) {
				if (mTempVg > mLastVg + 0.5) {
					mTempVg = mLastVg + 0.5;
				}
				if (mTempVg < mLastVg - 0.5) {
					mTempVg = mLastVg - 0.5;
				}
				if (mTempVs > mLastVs + 0.5) {
					mTempVs = mLastVs + 0.5;
				}
				if (mTempVs < mLastVs - 0.5) {
					mTempVs = mLastVs - 0.5;
				}
				if (mTempVd > mLastVd + 0.5) {
					mTempVd = mLastVd + 0.5;
				}
				if (mTempVd < mLastVd - 0.5) {
					mTempVd = mLastVd - 0.5;
				}
				if (CircuitElement.Converged && (NonConvergence(mLastVs, mTempVs) || NonConvergence(mLastVd, mTempVd) || NonConvergence(mLastVg, mTempVg))) {
					CircuitElement.Converged = false;
				}
			}
			mLastVg = mTempVg;
			mLastVs = mTempVs;
			mLastVd = mTempVd;

			double gds;
			double rs;
			{
				/* ドレインソース間電圧が負の場合
				 * ドレインとソースを入れ替える
				 * (電流の計算を単純化するため) */
				double vgs, vds;
				if (Nch * mTempVd < Nch * mTempVs) {
					vgs = mTempVg - mTempVd;
					vds = mTempVs - mTempVd;
				} else {
					vgs = mTempVg - mTempVs;
					vds = mTempVd - mTempVs;
				}
				var vgs_n = vgs * Nch;
				var vds_n = vds * Nch;
				if (vgs_n < Vth) {
					/* 遮断領域 */
					Mode = 0;
					Gm = 0;
					/* 電流を0にするべきだが特異な行列となるため
					 * 100MΩとした時の電流にする */
					gds = 1e-8;
					Current = vds_n * gds;
				} else if (vds_n < vgs_n - Vth) {
					/* 線形領域 */
					Mode = 1;
					Gm = Beta * vds_n;
					gds = Beta * (vgs_n - vds_n - Vth);
					Current = Beta * ((vgs_n - Vth) * vds_n - vds_n * vds_n * 0.5);
				} else {
					/* 飽和領域 */
					Mode = 2;
					Gm = Beta * (vgs_n - Vth);
					gds = 1e-8;
					Current = 0.5 * Beta * (vgs_n - Vth) * (vgs_n - Vth) + (vds_n - (vgs_n - Vth)) * gds;
				}
				rs = gds * vds + Gm * vgs - Current * Nch;
			}

			/* ドレインとソースを入れ替えている場合、電流を反転 */
			if (Nch * mTempVd < Nch * mTempVs) {
				Current = -Current;
			}

			if (MOS) {
				DiodeDoIteration(Nch * (Volts[mBodyTerminal] - Volts[IdxS]), ref mDiodeLastVdiff1, mDiodeNodes1A, mDiodeNodes1B);
				DiodeCurrent1 = DiodeCalculateCurrent(Nch * (Volts[mBodyTerminal] - Volts[IdxS])) * Nch;
				DiodeDoIteration(Nch * (Volts[mBodyTerminal] - Volts[IdxD]), ref mDiodeLastVdiff2, mDiodeNodes2A, mDiodeNodes2B);
				DiodeCurrent2 = DiodeCalculateCurrent(Nch * (Volts[mBodyTerminal] - Volts[IdxD])) * Nch;
			} else {
				DiodeCurrent1 = DiodeCurrent2 = 0;
			}

			CircuitElement.StampMatrix(Nodes[IdxD], Nodes[IdxD], gds);
			CircuitElement.StampMatrix(Nodes[IdxD], Nodes[IdxS], -gds - Gm);
			CircuitElement.StampMatrix(Nodes[IdxD], Nodes[IdxG], Gm);

			CircuitElement.StampMatrix(Nodes[IdxS], Nodes[IdxD], -gds);
			CircuitElement.StampMatrix(Nodes[IdxS], Nodes[IdxS], gds + Gm);
			CircuitElement.StampMatrix(Nodes[IdxS], Nodes[IdxG], -Gm);

			CircuitElement.StampRightSide(Nodes[IdxD], rs);
			CircuitElement.StampRightSide(Nodes[IdxS], -rs);
		}

		bool NonConvergence(double last, double now) {
			var diff = Math.Abs(last - now);
			if (Beta > 1) {
				// high beta MOSFETs are more sensitive to small differences, so we are more strict about convergence testing
				diff *= 100;
			}
			if (diff < 0.01) {
				// difference of less than 10mV is fine
				return false;
			}
			if (CircuitElement.SubIterations > 10 && diff < Math.Abs(now)*0.001) {
				// larger differences are fine if value is large
				return false;
			}
			if (CircuitElement.SubIterations > 100 && diff < 0.01+(CircuitElement.SubIterations-100)*0.0001) {
				// if we're having trouble converging, get more lenient
				return false;
			}
			return true;
		}
	}
}
