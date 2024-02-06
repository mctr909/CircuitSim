using System;

namespace Circuit.Elements.Active {
	class ElmFET : BaseElement {
		const int IdxG = 0;
		const int IdxS = 1;
		const int IdxD = 2;

		const double BackwardCompatibilityBeta = 1;
		const double DiodeVcrit = 0.6347668814648425;
		const double DiodeVscale = 0.05173;
		const double DiodeLeakage = 1.7143528192808883E-07;
		const double DiodeVdCoef = 19.331142470520007;

		public static double LastBeta;

		public static double DefaultBeta {
			get { return LastBeta == 0 ? BackwardCompatibilityBeta : LastBeta; }
		}

		public double Vth;
		public double Beta;

		public int Nch { get; private set; }
		public bool MOS { get; private set; }
		public int BodyTerminal { get; private set; }
		public int Mode { get; private set; } = 0;
		public double Gm { get; private set; } = 0;

		public double DiodeCurrent1 { get; private set; }
		public double DiodeCurrent2 { get; private set; }
		public double Vg { get { return Volts[IdxG]; } }
		public double Vs { get { return Volts[IdxS]; } }
		public double Vd { get { return Volts[IdxD]; } }

		double mDiodeLastVoltDiff = 0.0;
		double mLastVs = 0.0;
		double mLastVd = 0.0;
		int mDiodeNodeS;
		int mDiodeNodeD;

		public ElmFET(bool isNch, bool mos, double vth, double beta) : base() {
			Nch = isNch ? 1 : -1;
			MOS = mos;
			Vth = vth;
			Beta = beta;
			AllocNodes();
		}

		public override int TermCount { get { return 3; } }

		public override double VoltageDiff { get { return Volts[IdxG] - Volts[IdxS]; } }

		public override void Reset() {
			Volts[IdxG] = Volts[IdxS] = Volts[IdxD] = 0;
			mLastVs = 0.0;
			mLastVd = 0.0;
			mDiodeLastVoltDiff = 0.0;
			DiodeCurrent1 = 0.0;
			DiodeCurrent2 = 0.0;
		}

		public override bool GetConnection(int n1, int n2) { return !(n1 == 0 || n2 == 0); }

		public override void Stamp() {
			mDiodeNodeS = Nodes[IdxS];
			mDiodeNodeD = Nodes[IdxD];
			Circuit.StampNonLinear(mDiodeNodeS);
			Circuit.StampNonLinear(mDiodeNodeD);
			BodyTerminal = (Nch < 0) ? IdxD : IdxS;
		}

		public override double GetCurrentIntoNode(int n) {
			if (n == 0) {
				return 0;
			}
			if (n == 3) {
				return -DiodeCurrent1 - DiodeCurrent2;
			}
			if (n == 1) {
				return Current + DiodeCurrent1;
			}
			return -Current + DiodeCurrent2;
		}

		public override void DoIteration() {
			calc(false);
		}

		public override void IterationFinished() {
			calc(true);
			if (BodyTerminal == IdxS) {
				DiodeCurrent1 = -DiodeCurrent2;
			}
			if (BodyTerminal == IdxD) {
				DiodeCurrent2 = -DiodeCurrent1;
			}
		}

		void calc(bool finished) {
			var vs = Volts[IdxS];
			var vd = Volts[IdxD];
			if (!finished) {
				if (vs > mLastVs + 0.5) {
					vs = mLastVs + 0.5;
				}
				if (vs < mLastVs - 0.5) {
					vs = mLastVs - 0.5;
				}
				if (vd > mLastVd + 0.5) {
					vd = mLastVd + 0.5;
				}
				if (vd < mLastVd - 0.5) {
					vd = mLastVd - 0.5;
				}
				Volts[IdxS] = vs;
				Volts[IdxD] = vd;
			}
			mLastVs = vs;
			mLastVd = vd;

			/* ドレインソース間電圧が負の場合
			 * ドレインとソースを入れ替える
			 * (電流の計算を単純化するため) */
			int idxS, idxD;
			if (Nch * Volts[IdxD] < Nch * Volts[IdxS]) {
				idxS = IdxD;
				idxD = IdxS;
			} else {
				idxS = IdxS;
				idxD = IdxD;
			}
			var real_vgs = Volts[IdxG] - Volts[idxS];
			var real_vds = Volts[idxD] - Volts[idxS];
			double gds;
			{
				var vgs = real_vgs * Nch;
				var vds = real_vds * Nch;
				if (vgs < Vth) {
					/* 遮断領域 */
					/* 電流を0にするべきだが特異な行列となるため
					 * 100MΩとした時の電流にする */
					gds = 1e-8;
					Current = vds * gds;
					Gm = 0;
					Mode = 0;
				} else if (vds < vgs - Vth) {
					/* 線形領域 */
					gds = Beta * (vgs - vds - Vth);
					Current = Beta * ((vgs - Vth) * vds - vds * vds * .5);
					Gm = Beta * vds;
					Mode = 1;
				} else {
					/* 飽和領域 */
					gds = 1e-8;
					Current = .5 * Beta * (vgs - Vth) * (vgs - Vth) + (vds - (vgs - Vth)) * gds;
					Gm = Beta * (vgs - Vth);
					Mode = 2;
				}
			}

			var rs = gds * real_vds + Gm * real_vgs - Nch * Current;

			/* ドレインソース間電圧が負の場合
			 * ドレインとソースを入れ替えているため電流を反転 */
			if (idxS == 2 && Nch == 1 || idxS == 1 && Nch == -1) {
				Current = -Current;
			}

			if (MOS) {
				/* 還流ダイオード */
				if (Nch < 0) {
					var vbs = (Volts[BodyTerminal] - Volts[IdxS]) * Nch;
					DiodeDoStep(mDiodeNodeS, mDiodeNodeD, vbs, ref mDiodeLastVoltDiff);
					DiodeCurrent1 = (Math.Exp(vbs * DiodeVdCoef) - 1) * DiodeLeakage * Nch;
					DiodeCurrent2 = -DiodeCurrent1;
				} else {
					var vbd = (Volts[BodyTerminal] - Volts[IdxD]) * Nch;
					DiodeDoStep(mDiodeNodeS, mDiodeNodeD, vbd, ref mDiodeLastVoltDiff);
					DiodeCurrent2 = (Math.Exp(vbd * DiodeVdCoef) - 1) * DiodeLeakage * Nch;
					DiodeCurrent1 = -DiodeCurrent2;
				}
			} else {
				DiodeCurrent1 = DiodeCurrent2 = 0;
			}

			Circuit.StampMatrix(Nodes[IdxD], Nodes[IdxD], gds);
			Circuit.StampMatrix(Nodes[IdxD], Nodes[IdxS], -gds - Gm);
			Circuit.StampMatrix(Nodes[IdxD], Nodes[IdxG], Gm);

			Circuit.StampMatrix(Nodes[IdxS], Nodes[IdxD], -gds);
			Circuit.StampMatrix(Nodes[IdxS], Nodes[IdxS], gds + Gm);
			Circuit.StampMatrix(Nodes[IdxS], Nodes[IdxG], -Gm);

			Circuit.StampRightSide(Nodes[IdxD], rs);
			Circuit.StampRightSide(Nodes[IdxS], -rs);
		}

		static void DiodeDoStep(int n0, int n1, double voltdiff, ref double lastVoltDiff) {
			if (0.001 < Math.Abs(voltdiff - lastVoltDiff)) {
				Circuit.Converged = false;
			}

			var v_new = voltdiff;
			var v_old = lastVoltDiff;
			/* check new voltage; has current changed by factor of e^2? */
			if (v_new > DiodeVcrit && Math.Abs(v_new - v_old) > (DiodeVscale + DiodeVscale)) {
				if (v_old > 0) {
					var arg = 1 + (v_new - v_old) / DiodeVscale;
					if (arg > 0) {
						/* adjust vnew so that the current is the same
                         * as in linearized model from previous iteration.
                         * current at vnew = old current * arg */
						v_new = v_old + DiodeVscale * Math.Log(arg);
					} else {
						v_new = DiodeVcrit;
					}
				} else {
					/* adjust vnew so that the current is the same
                     * as in linearized model from previous iteration.
                     * (1/vscale = slope of load line) */
					v_new = DiodeVscale * Math.Log(v_new / DiodeVscale);
				}
				Circuit.Converged = false;
			}
			voltdiff = v_new;
			lastVoltDiff = v_new;

			/* regular diode or forward-biased zener */
			const double gmin = DiodeLeakage * 0.01;
			var eval = Math.Exp(voltdiff * DiodeVdCoef);
			var geq = DiodeVdCoef * DiodeLeakage * eval + gmin;
			var nc = (eval - 1) * DiodeLeakage - geq * voltdiff;

			n0--;
			n1--;
			var ri0 = Circuit.RowInfo[n0];
			var ri1 = Circuit.RowInfo[n1];
			var row0 = ri0.MapRow;
			var row1 = ri1.MapRow;
			if (ri0.IsConst) {
				Circuit.RightSide[row0] -= geq * ri0.Value;
				Circuit.RightSide[row1] += geq * ri0.Value;
			} else {
				Circuit.Matrix[row0, ri0.MapCol] += geq;
				Circuit.Matrix[row1, ri0.MapCol] -= geq;
			}
			if (ri1.IsConst) {
				Circuit.RightSide[row0] += geq * ri1.Value;
				Circuit.RightSide[row1] -= geq * ri1.Value;
			} else {
				Circuit.Matrix[row0, ri1.MapCol] -= geq;
				Circuit.Matrix[row1, ri1.MapCol] += geq;
			}
			Circuit.RightSide[row0] -= nc;
			Circuit.RightSide[row1] += nc;
		}
	}
}
