namespace Circuit.Elements.Active {
	class ElmFET : BaseElement {
		public const int IdxG = 0;
		public const int IdxS = 1;
		public const int IdxD = 2;

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

		public double Vg { get { return V[IdxG]; } }
		public double Vs { get { return V[IdxS]; } }
		public double Vd { get { return V[IdxD]; } }

		public double mTempVg = 0.0;
		public double mTempVs = 0.0;
		public double mTempVd = 0.0;
		public double mLastVg = 0.0;
		public double mLastVs = 0.0;
		public double mLastVd = 0.0;

		public int mBodyTerminal;
		public int mDiodeNodes1A;
		public int mDiodeNodes1B;
		public int mDiodeNodes2A;
		public int mDiodeNodes2B;
		public double mDiodeLastVdiff1 = 0.0;
		public double mDiodeLastVdiff2 = 0.0;

		public override int TermCount { get { return 3; } }

		public override double VoltageDiff {
			get { return V[IdxG] - V[IdxS]; }
		}

		protected static void DiodeDoIteration(double vnew, ref double vold, int nodeA, int nodeB) {
			if (Math.Abs(vnew - vold) > 0.01) {
				CONVERGED = false;
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
				CONVERGED = false;
			}
			vold = vnew;
			var g_min = DiodeLeakage * 0.01;
			if (ITER_COUNT > 100) {
				g_min = Math.Exp(-9 * Math.Log(10) * (1 - ITER_COUNT / 3000.0));
				if (g_min > 0.1) {
					g_min = 0.1;
				}
			}
			var eval = Math.Exp(vnew * DiodeVdCoef);
			var geq = DiodeVdCoef * DiodeLeakage * eval + g_min;
			var nc = (eval - 1) * DiodeLeakage - geq * vnew;
			UpdateConductance(nodeA, nodeB, geq);
			UpdateCurrentSource(nodeA, nodeB, nc);
		}

		protected static double DiodeCalculateCurrent(double voltdiff) {
			return DiodeLeakage * (Math.Exp(voltdiff * DiodeVdCoef) - 1);
		}

		void Calc(bool finished) {
			mTempVg = V[IdxG];
			mTempVs = V[IdxS];
			mTempVd = V[IdxD];
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
				if (CONVERGED && (NonConvergence(mLastVs, mTempVs) || NonConvergence(mLastVd, mTempVd) || NonConvergence(mLastVg, mTempVg))) {
					CONVERGED = false;
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
					I[0] = vds_n * gds;
				} else if (vds_n < vgs_n - Vth) {
					/* 線形領域 */
					Mode = 1;
					Gm = Beta * vds_n;
					gds = Beta * (vgs_n - vds_n - Vth);
					I[0] = Beta * ((vgs_n - Vth) * vds_n - vds_n * vds_n * 0.5);
				} else {
					/* 飽和領域 */
					Mode = 2;
					Gm = Beta * (vgs_n - Vth);
					gds = 1e-8;
					I[0] = 0.5 * Beta * (vgs_n - Vth) * (vgs_n - Vth) + (vds_n - (vgs_n - Vth)) * gds;
				}
				rs = gds * vds + Gm * vgs - I[0] * Nch;
			}

			/* ドレインとソースを入れ替えている場合、電流を反転 */
			if (Nch * mTempVd < Nch * mTempVs) {
				I[0] = -I[0];
			}

			if (MOS) {
				DiodeDoIteration(Nch * (V[mBodyTerminal] - V[IdxS]), ref mDiodeLastVdiff1, mDiodeNodes1A, mDiodeNodes1B);
				DiodeCurrent1 = DiodeCalculateCurrent(Nch * (V[mBodyTerminal] - V[IdxS])) * Nch;
				DiodeDoIteration(Nch * (V[mBodyTerminal] - V[IdxD]), ref mDiodeLastVdiff2, mDiodeNodes2A, mDiodeNodes2B);
				DiodeCurrent2 = DiodeCalculateCurrent(Nch * (V[mBodyTerminal] - V[IdxD])) * Nch;
			} else {
				DiodeCurrent1 = DiodeCurrent2 = 0;
			}

			UpdateMatrix(Nodes[IdxD], Nodes[IdxD], gds);
			UpdateMatrix(Nodes[IdxD], Nodes[IdxS], -gds - Gm);
			UpdateMatrix(Nodes[IdxD], Nodes[IdxG], Gm);

			UpdateMatrix(Nodes[IdxS], Nodes[IdxD], -gds);
			UpdateMatrix(Nodes[IdxS], Nodes[IdxS], gds + Gm);
			UpdateMatrix(Nodes[IdxS], Nodes[IdxG], -Gm);

			UpdateCurrentSource(Nodes[IdxS], Nodes[IdxD], rs);
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
			if (ITER_COUNT > 10 && diff < Math.Abs(now)*0.001) {
				// larger differences are fine if value is large
				return false;
			}
			if (ITER_COUNT > 100 && diff < 0.01+(ITER_COUNT-100)*0.0001) {
				// if we're having trouble converging, get more lenient
				return false;
			}
			return true;
		}

		protected override void DoIteration() {
			Calc(false);
		}

		protected override void FinishIteration() {
			Calc(true);
			if (mBodyTerminal == IdxS) {
				DiodeCurrent1 = -DiodeCurrent2;
			}
			if (mBodyTerminal == IdxD) {
				DiodeCurrent2 = -DiodeCurrent1;
			}
		}

		protected override double GetCurrent(int n) {
			if (n == 0) {
				return 0;
			}
			if (n == 1) {
				return I[0] + DiodeCurrent1;
			}
			return -I[0] + DiodeCurrent2;
		}
	}
}
