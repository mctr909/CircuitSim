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

		public double Vg { get { return NodeVolts[IdxG]; } }
		public double Vs { get { return NodeVolts[IdxS]; } }
		public double Vd { get { return NodeVolts[IdxD]; } }

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

		public override double GetVoltageDiff() {
			return NodeVolts[IdxG] - NodeVolts[IdxS];
		}

		protected static void DiodeDoIteration(double vnew, ref double vold, int nodeA, int nodeB) {
			if (Math.Abs(vnew - vold) > 0.01) {
				CircuitState.Converged = false;
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
				CircuitState.Converged = false;
			}
			vold = vnew;
			var g_min = DiodeLeakage * 0.01;
			if (CircuitElement.ITER_COUNT > 100) {
				g_min = Math.Exp(-9 * Math.Log(10) * (1 - CircuitElement.ITER_COUNT / 3000.0));
				if (g_min > 0.1) {
					g_min = 0.1;
				}
			}
			var eval = Math.Exp(vnew * DiodeVdCoef);
			var geq = DiodeVdCoef * DiodeLeakage * eval + g_min;
			var nc = (eval - 1) * DiodeLeakage - geq * vnew;
			UpdateConductance(nodeA, nodeB, geq);
			UpdateCurrent(nodeA, nodeB, nc);
		}

		protected static double DiodeCalculateCurrent(double voltdiff) {
			return DiodeLeakage * (Math.Exp(voltdiff * DiodeVdCoef) - 1);
		}

		void Calc(bool finished) {
			mTempVg = NodeVolts[IdxG];
			mTempVs = NodeVolts[IdxS];
			mTempVd = NodeVolts[IdxD];
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
				if (CircuitState.Converged && (NonConvergence(mLastVs, mTempVs) || NonConvergence(mLastVd, mTempVd) || NonConvergence(mLastVg, mTempVg))) {
					CircuitState.Converged = false;
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
				DiodeDoIteration(Nch * (NodeVolts[mBodyTerminal] - NodeVolts[IdxS]), ref mDiodeLastVdiff1, mDiodeNodes1A, mDiodeNodes1B);
				DiodeCurrent1 = DiodeCalculateCurrent(Nch * (NodeVolts[mBodyTerminal] - NodeVolts[IdxS])) * Nch;
				DiodeDoIteration(Nch * (NodeVolts[mBodyTerminal] - NodeVolts[IdxD]), ref mDiodeLastVdiff2, mDiodeNodes2A, mDiodeNodes2B);
				DiodeCurrent2 = DiodeCalculateCurrent(Nch * (NodeVolts[mBodyTerminal] - NodeVolts[IdxD])) * Nch;
			} else {
				DiodeCurrent1 = DiodeCurrent2 = 0;
			}

			UpdateMatrix(NodeId[IdxD], NodeId[IdxD], gds);
			UpdateMatrix(NodeId[IdxD], NodeId[IdxS], -gds - Gm);
			UpdateMatrix(NodeId[IdxD], NodeId[IdxG], Gm);

			UpdateMatrix(NodeId[IdxS], NodeId[IdxD], -gds);
			UpdateMatrix(NodeId[IdxS], NodeId[IdxS], gds + Gm);
			UpdateMatrix(NodeId[IdxS], NodeId[IdxG], -Gm);

			UpdateCurrent(NodeId[IdxS], NodeId[IdxD], rs);
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
			if (CircuitElement.ITER_COUNT > 10 && diff < Math.Abs(now)*0.001) {
				// larger differences are fine if value is large
				return false;
			}
			if (CircuitElement.ITER_COUNT > 100 && diff < 0.01+(CircuitElement.ITER_COUNT-100)*0.0001) {
				// if we're having trouble converging, get more lenient
				return false;
			}
			return true;
		}

		#region [method(Analyze)]
		public override bool HasConnection(int n1, int n2) { return !(n1 == 0 || n2 == 0); }

		public override void Reset() {
			NodeVolts[IdxG] = NodeVolts[IdxS] = NodeVolts[IdxD] = 0;
			mLastVs = 0.0;
			mLastVd = 0.0;
			mLastVg = 0.0;
			mDiodeLastVdiff1 = 0;
			mDiodeLastVdiff2 = 0;
			DiodeCurrent1 = 0.0;
			DiodeCurrent2 = 0.0;
		}

		public override void Stamp() {
			StampNonLinear(NodeId[IdxS]);
			StampNonLinear(NodeId[IdxD]);
			mBodyTerminal = (Nch < 0) ? IdxD : IdxS;
			if (MOS) {
				if (Nch < 0) {
					mDiodeNodes1A = NodeId[IdxS];
					mDiodeNodes1B = NodeId[mBodyTerminal];
					mDiodeNodes2A = NodeId[IdxD];
					mDiodeNodes2B = NodeId[mBodyTerminal];
				} else {
					mDiodeNodes1A = NodeId[mBodyTerminal];
					mDiodeNodes1B = NodeId[IdxS];
					mDiodeNodes2A = NodeId[mBodyTerminal];
					mDiodeNodes2B = NodeId[IdxD];
				}
				StampNonLinear(mDiodeNodes1A);
				StampNonLinear(mDiodeNodes1B);
				StampNonLinear(mDiodeNodes2A);
				StampNonLinear(mDiodeNodes2B);
			}
		}
		#endregion

		#region [method(Circuit)]
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

		public override double GetCurrent(int n) {
			if (n == 0) {
				return 0;
			}
			if (n == 1) {
				return Current + DiodeCurrent1;
			}
			return -Current + DiodeCurrent2;
		}
		#endregion
	}
}
