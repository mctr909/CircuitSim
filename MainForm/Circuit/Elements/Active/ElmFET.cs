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

		public double Vg { get { return volts[IdxG]; } }
		public double Vs { get { return volts[IdxS]; } }
		public double Vd { get { return volts[IdxD]; } }

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

		public override double voltage_diff() {
			return volts[IdxG] - volts[IdxS];
		}

		protected static void DiodeDoIteration(double vnew, ref double vold, int nodeA, int nodeB) {
			if (Math.Abs(vnew - vold) > 0.01) {
				CircuitElement.converged = false;
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
				CircuitElement.converged = false;
			}
			vold = vnew;
			var gmin = DiodeLeakage * 0.01;
			if (CircuitElement.sub_iterations > 100) {
				gmin = Math.Exp(-9 * Math.Log(10) * (1 - CircuitElement.sub_iterations / 3000.0));
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
			mTempVg = volts[IdxG];
			mTempVs = volts[IdxS];
			mTempVd = volts[IdxD];
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
				if (CircuitElement.converged && (NonConvergence(mLastVs, mTempVs) || NonConvergence(mLastVd, mTempVd) || NonConvergence(mLastVg, mTempVg))) {
					CircuitElement.converged = false;
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
					current = vds_n * gds;
				} else if (vds_n < vgs_n - Vth) {
					/* 線形領域 */
					Mode = 1;
					Gm = Beta * vds_n;
					gds = Beta * (vgs_n - vds_n - Vth);
					current = Beta * ((vgs_n - Vth) * vds_n - vds_n * vds_n * 0.5);
				} else {
					/* 飽和領域 */
					Mode = 2;
					Gm = Beta * (vgs_n - Vth);
					gds = 1e-8;
					current = 0.5 * Beta * (vgs_n - Vth) * (vgs_n - Vth) + (vds_n - (vgs_n - Vth)) * gds;
				}
				rs = gds * vds + Gm * vgs - current * Nch;
			}

			/* ドレインとソースを入れ替えている場合、電流を反転 */
			if (Nch * mTempVd < Nch * mTempVs) {
				current = -current;
			}

			if (MOS) {
				DiodeDoIteration(Nch * (volts[mBodyTerminal] - volts[IdxS]), ref mDiodeLastVdiff1, mDiodeNodes1A, mDiodeNodes1B);
				DiodeCurrent1 = DiodeCalculateCurrent(Nch * (volts[mBodyTerminal] - volts[IdxS])) * Nch;
				DiodeDoIteration(Nch * (volts[mBodyTerminal] - volts[IdxD]), ref mDiodeLastVdiff2, mDiodeNodes2A, mDiodeNodes2B);
				DiodeCurrent2 = DiodeCalculateCurrent(Nch * (volts[mBodyTerminal] - volts[IdxD])) * Nch;
			} else {
				DiodeCurrent1 = DiodeCurrent2 = 0;
			}

			CircuitElement.StampMatrix(node_index[IdxD], node_index[IdxD], gds);
			CircuitElement.StampMatrix(node_index[IdxD], node_index[IdxS], -gds - Gm);
			CircuitElement.StampMatrix(node_index[IdxD], node_index[IdxG], Gm);

			CircuitElement.StampMatrix(node_index[IdxS], node_index[IdxD], -gds);
			CircuitElement.StampMatrix(node_index[IdxS], node_index[IdxS], gds + Gm);
			CircuitElement.StampMatrix(node_index[IdxS], node_index[IdxG], -Gm);

			CircuitElement.StampRightSide(node_index[IdxD], rs);
			CircuitElement.StampRightSide(node_index[IdxS], -rs);
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
			if (CircuitElement.sub_iterations > 10 && diff < Math.Abs(now)*0.001) {
				// larger differences are fine if value is large
				return false;
			}
			if (CircuitElement.sub_iterations > 100 && diff < 0.01+(CircuitElement.sub_iterations-100)*0.0001) {
				// if we're having trouble converging, get more lenient
				return false;
			}
			return true;
		}

		#region [method(Analyze)]
		public override bool has_connection(int n1, int n2) { return !(n1 == 0 || n2 == 0); }

		public override void reset() {
			volts[IdxG] = volts[IdxS] = volts[IdxD] = 0;
			mLastVs = 0.0;
			mLastVd = 0.0;
			mLastVg = 0.0;
			mDiodeLastVdiff1 = 0;
			mDiodeLastVdiff2 = 0;
			DiodeCurrent1 = 0.0;
			DiodeCurrent2 = 0.0;
		}

		public override void stamp() {
			CircuitElement.StampNonLinear(node_index[IdxS]);
			CircuitElement.StampNonLinear(node_index[IdxD]);
			mBodyTerminal = (Nch < 0) ? IdxD : IdxS;
			if (MOS) {
				if (Nch < 0) {
					mDiodeNodes1A = node_index[IdxS];
					mDiodeNodes1B = node_index[mBodyTerminal];
					mDiodeNodes2A = node_index[IdxD];
					mDiodeNodes2B = node_index[mBodyTerminal];
				} else {
					mDiodeNodes1A = node_index[mBodyTerminal];
					mDiodeNodes1B = node_index[IdxS];
					mDiodeNodes2A = node_index[mBodyTerminal];
					mDiodeNodes2B = node_index[IdxD];
				}
				CircuitElement.StampNonLinear(mDiodeNodes1A);
				CircuitElement.StampNonLinear(mDiodeNodes1B);
				CircuitElement.StampNonLinear(mDiodeNodes2A);
				CircuitElement.StampNonLinear(mDiodeNodes2B);
			}
		}
		#endregion

		#region [method(Circuit)]
		public override void do_iteration() {
			Calc(false);
		}

		public override void finish_iteration() {
			Calc(true);
			if (mBodyTerminal == IdxS) {
				DiodeCurrent1 = -DiodeCurrent2;
			}
			if (mBodyTerminal == IdxD) {
				DiodeCurrent2 = -DiodeCurrent1;
			}
		}

		public override double get_current_into_node(int n) {
			if (n == 0) {
				return 0;
			}
			if (n == 1) {
				return current + DiodeCurrent1;
			}
			return -current + DiodeCurrent2;
		}
		#endregion
	}
}
