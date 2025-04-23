namespace Circuit.Elements.Active {
	class ElmFET : BaseElement {
		public const int G = 0;
		public const int S = 1;
		public const int D = 2;
		public const int G_LAST = 3;
		public const int S_LAST = 4;
		public const int D_LAST = 5;
		public const int VD_D1 = 6;
		public const int VD_D2 = 7;

		public const int CUR_D1 = 1;
		public const int CUR_D2 = 2;

		public const int N_CH = 0;
		public const int V_TH = 1;
		public const int BETA = 2;
		public const int GM = 3;

		public const int MOS = 0;

		const double DiodeVScale = 0.05173;
		const double DiodeVdCoef = 19.331142470520007;
		const double DiodeLeakage = 1.7143528192808883E-07;
		const double DiodeVCrit = 6.34767e-01; //DiodeVScale * Math.Log(DiodeVScale / (Math.Sqrt(2) * DiodeLeakage));

		public int BODY;
		public int D1_A;
		public int D1_B;
		public int D2_A;
		public int D2_B;

		public override int TermCount { get { return 3; } }

		public override double VoltageDiff {
			get { return V[G] - V[S]; }
		}

		protected static void DiodeDoIteration(double vdNew, ref double vdOld, int nodeA, int nodeB) {
			if (Math.Abs(vdNew - vdOld) > 0.01) {
				CONVERGED = false;
			}
			if (vdNew > DiodeVCrit && Math.Abs(vdNew - vdOld) > (DiodeVScale + DiodeVScale)) {
				if (vdOld > 0) {
					var arg = 1 + (vdNew - vdOld) / DiodeVScale;
					if (arg > 0) {
						vdNew = vdOld + DiodeVScale * Math.Log(arg);
					} else {
						vdNew = DiodeVCrit;
					}
				} else {
					vdNew = DiodeVScale * Math.Log(vdNew / DiodeVScale);
				}
				CONVERGED = false;
			}
			vdOld = vdNew;
			var gMin = DiodeLeakage * 0.01;
			//if (ITER_COUNT > 100) {
			//	gMin = Math.Exp(-9 * Math.Log(10) * (1 - ITER_COUNT / 3000.0));
			//	if (gMin > 0.1) {
			//		gMin = 0.1;
			//	}
			//}
			var vdExp = Math.Exp(vdNew * DiodeVdCoef);
			var geq = DiodeVdCoef * vdExp * DiodeLeakage + gMin;
			var nc = (vdExp - 1) * DiodeLeakage - geq * vdNew;
			UpdateConductance(nodeA, nodeB, geq);
			UpdateCurrentSource(nodeA, nodeB, nc);
		}

		protected static double DiodeCalculateCurrent(double voltdiff) {
			return DiodeLeakage * (Math.Exp(voltdiff * DiodeVdCoef) - 1);
		}

		void Calc(bool finished) {
			var vg = V[G];
			var vs = V[S];
			var vd = V[D];
			if (!finished) {
				var lg = V[G_LAST];
				if (vg > lg + 0.5) {
					vg = lg + 0.5;
				}
				if (vg < lg - 0.5) {
					vg = lg - 0.5;
				}
				var ls = V[S_LAST];
				if (vs > ls + 0.5) {
					vs = ls + 0.5;
				}
				if (vs < ls - 0.5) {
					vs = ls - 0.5;
				}
				var ld = V[D_LAST];
				if (vd > ld + 0.5) {
					vd = ld + 0.5;
				}
				if (vd < ld - 0.5) {
					vd = ld - 0.5;
				}
				if (CONVERGED && (NonConvergence(ls, vs) || NonConvergence(ld, vd) || NonConvergence(lg, vg))) {
					CONVERGED = false;
				}
			}
			V[G_LAST] = vg;
			V[S_LAST] = vs;
			V[D_LAST] = vd;

			var nCh = Para[N_CH];
			double gds;
			double rs;
			{
				/* ドレインソース間電圧が負の場合
				 * ドレインとソースを入れ替える
				 * (電流の計算を単純化するため) */
				double vgs, vds;
				if (nCh * vd < nCh * vs) {
					vgs = vg - vd;
					vds = vs - vd;
				} else {
					vgs = vg - vs;
					vds = vd - vs;
				}
				var vgs_n = vgs * nCh;
				var vds_n = vds * nCh;
				var vth = Para[V_TH];
				var beta = Para[BETA];
				if (vgs_n < vth) {
					/* 遮断領域 */
					Para[GM] = 0;
					/* 電流を0にするべきだが特異な行列となるため
					 * 100MΩとした時の電流にする */
					gds = 1e-8;
					I[0] = vds_n * gds;
				} else if (vds_n < vgs_n - vth) {
					/* 線形領域 */
					Para[GM] = beta * vds_n;
					gds = beta * (vgs_n - vds_n - vth);
					I[0] = beta * ((vgs_n - vth) * vds_n - vds_n * vds_n * 0.5);
				} else {
					/* 飽和領域 */
					Para[GM] = beta * (vgs_n - vth);
					gds = 1e-8;
					I[0] = 0.5 * beta * (vgs_n - vth) * (vgs_n - vth) + (vds_n - (vgs_n - vth)) * gds;
				}
				rs = gds * vds + Para[GM] * vgs - I[0] * nCh;
			}

			/* ドレインとソースを入れ替えている場合、電流を反転 */
			if (nCh * vd < nCh * vs) {
				I[0] = -I[0];
			}

			if (State[MOS] != 0) {
				DiodeDoIteration(nCh * (V[BODY] - V[S]), ref V[VD_D1], Nodes[D1_A], Nodes[D1_B]);
				I[CUR_D1] = DiodeCalculateCurrent(nCh * (V[BODY] - V[S])) * nCh;
				DiodeDoIteration(nCh * (V[BODY] - V[D]), ref V[VD_D2], Nodes[D2_A], Nodes[D2_B]);
				I[CUR_D2] = DiodeCalculateCurrent(nCh * (V[BODY] - V[D])) * nCh;
			} else {
				I[CUR_D1] = I[CUR_D2] = 0;
			}

			var gm = Para[GM];
			UpdateMatrix(Nodes[D], Nodes[D], gds);
			UpdateMatrix(Nodes[D], Nodes[S], -gds - gm);
			UpdateMatrix(Nodes[D], Nodes[G], gm);

			UpdateMatrix(Nodes[S], Nodes[D], -gds);
			UpdateMatrix(Nodes[S], Nodes[S], gds + gm);
			UpdateMatrix(Nodes[S], Nodes[G], -gm);

			UpdateCurrentSource(Nodes[S], Nodes[D], rs);
		}

		bool NonConvergence(double vOld, double vNew) {
			var diff = Math.Abs(vNew - vOld);
			if (Para[BETA] > 1) {
				// high beta MOSFETs are more sensitive to small differences, so we are more strict about convergence testing
				diff *= 100;
			}
			if (diff < 0.01) {
				// difference of less than 10mV is fine
				return false;
			}
			if (ITER_COUNT > 10 && diff < Math.Abs(vNew) * 0.001) {
				// larger differences are fine if value is large
				return false;
			}
			if (ITER_COUNT > 100 && diff < 0.01 + (ITER_COUNT - 100) * 0.0001) {
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
			if (BODY == S) {
				I[CUR_D1] = -I[CUR_D2];
			}
			if (BODY == D) {
				I[CUR_D2] = -I[CUR_D1];
			}
		}

		protected override double GetCurrent(int n) {
			if (n == 0) {
				return 0;
			}
			if (n == 1) {
				return I[0] + I[CUR_D1];
			}
			return -I[0] + I[CUR_D2];
		}
	}
}
