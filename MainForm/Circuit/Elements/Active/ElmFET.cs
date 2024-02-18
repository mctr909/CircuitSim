namespace Circuit.Elements.Active {
	class ElmFET : BaseElement {
		protected const int IdxG = 0;
		protected const int IdxS = 1;
		protected const int IdxD = 2;

		const double DiodeVScale = 0.05173;
		const double DiodeVdCoef = 19.331142470520007;
		const double DiodeLeakage = 1.7143528192808883E-07;

		public int Nch;
		public bool MOS;
		public double Vth;
		public double Beta;
		public int BodyTerminal;
		public int Mode;
		public double Gm;

		public double DiodeCurrent1 { get; private set; }
		public double DiodeCurrent2 { get; private set; }
		public double Vg { get { return Volts[IdxG]; } }
		public double Vs { get { return Volts[IdxS]; } }
		public double Vd { get { return Volts[IdxD]; } }

		int[] mDiodeNodes1 = new int[2];
		int[] mDiodeNodes2 = new int[2];
		double mVCrit = DiodeVScale * Math.Log(DiodeVScale / (Math.Sqrt(2) * DiodeLeakage));
		double mDiodeLastVdiff1 = 0.0;
		double mDiodeLastVdiff2 = 0.0;

		double mLastVs = 0.0;
		double mLastVd = 0.0;
		double mLastVg = 0.0;

		public override int TermCount { get { return 3; } }

		public override double VoltageDiff { get { return Volts[IdxG] - Volts[IdxS]; } }

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

		public override bool GetConnection(int n1, int n2) { return !(n1 == 0 || n2 == 0); }

		public override void Stamp() {
			CircuitElement.StampNonLinear(Nodes[IdxS]);
			CircuitElement.StampNonLinear(Nodes[IdxD]);
			BodyTerminal = (Nch < 0) ? IdxD : IdxS;
			if (MOS) {
				if (Nch < 0) {
					mDiodeNodes1[0] = Nodes[IdxS];
					mDiodeNodes1[1] = Nodes[BodyTerminal];
					mDiodeNodes2[0] = Nodes[IdxD];
					mDiodeNodes2[1] = Nodes[BodyTerminal];
				} else {
					mDiodeNodes1[0] = Nodes[BodyTerminal];
					mDiodeNodes1[1] = Nodes[IdxS];
					mDiodeNodes2[0] = Nodes[BodyTerminal];
					mDiodeNodes2[1] = Nodes[IdxD];
				}
				CircuitElement.StampNonLinear(mDiodeNodes1[0]);
				CircuitElement.StampNonLinear(mDiodeNodes1[1]);
				CircuitElement.StampNonLinear(mDiodeNodes2[0]);
				CircuitElement.StampNonLinear(mDiodeNodes2[1]);
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

		public override void IterationFinished() {
			Calc(true);
			if (BodyTerminal == IdxS) {
				DiodeCurrent1 = -DiodeCurrent2;
			}
			if (BodyTerminal == IdxD) {
				DiodeCurrent2 = -DiodeCurrent1;
			}
		}

		protected void DiodeDoIteration(double vnew, ref double vold, int nodeA, int nodeB) {
			if (Math.Abs(vnew - vold) > 0.01) {
				CircuitElement.Converged = false;
			}
			if (vnew > mVCrit && Math.Abs(vnew - vold) > (DiodeVScale + DiodeVScale)) {
				if (vold > 0) {
					var arg = 1 + (vnew - vold) / DiodeVScale;
					if (arg > 0) {
						vnew = vold + DiodeVScale * Math.Log(arg);
					} else {
						vnew = mVCrit;
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
			var v = new double[] { Volts[0], Volts[1], Volts[2] };
			if (!finished) {
				if (v[IdxG] > mLastVg + 0.5) {
					v[IdxG] = mLastVg + 0.5;
				}
				if (v[IdxG] < mLastVg - 0.5) {
					v[IdxG] = mLastVg - 0.5;
				}
				if (v[IdxS] > mLastVs + 0.5) {
					v[IdxS] = mLastVs + 0.5;
				}
				if (v[IdxS] < mLastVs - 0.5) {
					v[IdxS] = mLastVs - 0.5;
				}
				if (v[IdxD] > mLastVd + 0.5) {
					v[IdxD] = mLastVd + 0.5;
				}
				if (v[IdxD] < mLastVd - 0.5) {
					v[IdxD] = mLastVd - 0.5;
				}
				if (CircuitElement.Converged && (NonConvergence(mLastVs, v[IdxS]) || NonConvergence(mLastVd, v[IdxD]) || NonConvergence(mLastVg, v[IdxG]))) {
					CircuitElement.Converged = false;
				}
			}
			mLastVg = v[IdxG];
			mLastVs = v[IdxS];
			mLastVd = v[IdxD];

			/* ドレインソース間電圧が負の場合
			 * ドレインとソースを入れ替える
			 * (電流の計算を単純化するため) */
			int idxS, idxD;
			if (Nch * v[IdxD] < Nch * v[IdxS]) {
				idxS = IdxD;
				idxD = IdxS;
			} else {
				idxS = IdxS;
				idxD = IdxD;
			}
			var vgs = v[IdxG] - v[idxS];
			var vds = v[idxD] - v[idxS];

			double gds;
			{
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
			}

			var rs = gds * vds + Gm * vgs - Current * Nch;

			/* ドレインとソースを入れ替えている場合、電流を反転 */
			if (idxS == IdxD && Nch == 1 || idxS == IdxS && Nch == -1) {
				Current = -Current;
			}

			if (MOS) {
				DiodeDoIteration(Nch * (Volts[BodyTerminal] - Volts[IdxS]), ref mDiodeLastVdiff1, mDiodeNodes1[0], mDiodeNodes1[1]);
				DiodeCurrent1 = DiodeCalculateCurrent(Nch * (Volts[BodyTerminal] - Volts[IdxS])) * Nch;
				DiodeDoIteration(Nch * (Volts[BodyTerminal] - Volts[IdxD]), ref mDiodeLastVdiff2, mDiodeNodes2[0], mDiodeNodes2[1]);
				DiodeCurrent2 = DiodeCalculateCurrent(Nch * (Volts[BodyTerminal] - Volts[IdxD])) * Nch;
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
