using System;

namespace Circuit.Elements.Active {
	class ElmMosfet : BaseElement {
		const int IdxG = 0;
		const int IdxS = 1;
		const int IdxD = 2;

		const double BackwardCompatibilityBeta = 1;
		const double DiodeVcrit = 0.6347668814648425;
		const double DiodeVscale = 0.05173;
		const double DiodeLeakage = 1.7143528192808883E-07;
		const double DiodeVdCoef = 19.331142470520007;

		public const double DefaultThreshold = 1.5;

		public static double LastBeta;

		public static double DefaultBeta {
			get { return LastBeta == 0 ? BackwardCompatibilityBeta : LastBeta; }
		}

		public double Vth;
		public double Beta;

		public int Nch { get; private set; }
		public int BodyTerminal { get; private set; }
		public int Mode { get; private set; } = 0;
		public double Gm { get; private set; } = 0;

		public double DiodeCurrent1 { get; private set; }
		public double DiodeCurrent2 { get; private set; }
		public double Vg { get { return Volts[IdxG]; } }
		public double Vs { get { return Volts[IdxS]; } }
		public double Vd { get { return Volts[IdxD]; } }

		double mDiodeLastVoltDiff = 0.0;
		int mDiodeNodeS;
		int mDiodeNodeD;

		public ElmMosfet(bool pChFlag) : base() {
			Nch = pChFlag ? -1 : 1;
			Beta = DefaultBeta;
			Vth = DefaultThreshold;
		}
		public ElmMosfet(bool pChFlag, double vth, double beta) : base() {
			Nch = pChFlag ? -1 : 1;
			Vth = vth;
			Beta = beta;
			AllocNodes();
		}

		public override int TermCount { get { return 3; } }

		public override double VoltageDiff { get { return Volts[IdxG] - Volts[IdxS]; } }

		public override void Reset() {
			Volts[IdxG] = Volts[IdxS] = Volts[IdxD] = 0;
			mDiodeLastVoltDiff = 0.0;
		}

		public override bool GetConnection(int n1, int n2) { return !(n1 == 0 || n2 == 0); }

		public override void Stamp() {
			BodyTerminal = (Nch < 0) ? IdxD : IdxS;
			mDiodeNodeS = Nodes[IdxS];
			mDiodeNodeD = Nodes[IdxD];
			Circuit.RowInfo[Nodes[IdxS] - 1].LeftChanges = true;
			Circuit.RowInfo[Nodes[IdxD] - 1].LeftChanges = true;
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

			double gds;
			double rs;
			{
				var vgs = Volts[IdxG] - Volts[idxS];
				var vds = Volts[idxD] - Volts[idxS];
				var nvgs_vth = vgs * Nch - Vth;
				var nvds = vds * Nch;
				if (nvgs_vth < 0.0) {
					/* 遮断領域 */
					/* 電流を0にするべきだが特異な行列となるため
                     * 100MΩとした時の電流にする */
					gds = 1e-8;
					Gm = 0;
					Current = nvds * gds;
					Mode = 0;
				} else if (nvds < nvgs_vth) {
					/* 線形領域 */
					gds = Beta * (nvgs_vth - nvds);
					Gm = Beta * nvds;
					Current = Beta * (nvgs_vth * nvds - nvds * nvds * 0.5);
					Mode = 1;
				} else {
					/* 飽和領域 */
					gds = 1e-8;
					Gm = Beta * nvgs_vth;
					Current = 0.5 * Beta * nvgs_vth * nvgs_vth + (nvds - nvgs_vth) * gds;
					Mode = 2;
				}
				rs = gds * vds + Gm * vgs - Nch * Current;
			}

			/* ドレインソース間電圧が負の場合
             * ドレインとソースを入れ替えているため電流を反転 */
			if (idxS == 2 && Nch == 1 || idxS == 1 && Nch == -1) {
				Current = -Current;
			}

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

			{
				var riD = Circuit.RowInfo[Nodes[idxD] - 1];
				var riS = Circuit.RowInfo[Nodes[idxS] - 1];
				var rowD = riD.MapRow;
				var rowS = riS.MapRow;
				if (riD.IsConst) {
					Circuit.RightSide[rowD] -= gds * riD.Value;
					Circuit.RightSide[rowS] += gds * riD.Value;
				} else {
					Circuit.Matrix[rowD, riD.MapCol] += gds;
					Circuit.Matrix[rowS, riD.MapCol] -= gds;
				}
				if (riS.IsConst) {
					Circuit.RightSide[rowD] += (gds + Gm) * riS.Value;
					Circuit.RightSide[rowS] -= (gds + Gm) * riS.Value;
				} else {
					Circuit.Matrix[rowD, riS.MapCol] -= gds + Gm;
					Circuit.Matrix[rowS, riS.MapCol] += gds + Gm;
				}
				var riG = Circuit.RowInfo[Nodes[IdxG] - 1];
				if (riG.IsConst) {
					Circuit.RightSide[rowD] -= Gm * riG.Value;
					Circuit.RightSide[rowS] += Gm * riG.Value;
				} else {
					Circuit.Matrix[rowD, riG.MapCol] += Gm;
					Circuit.Matrix[rowS, riG.MapCol] -= Gm;
				}
				Circuit.RightSide[rowD] += rs;
				Circuit.RightSide[rowS] -= rs;
			}
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
