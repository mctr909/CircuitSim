namespace Circuit.Elements.Active {
	class ElmBJT : BaseElement {
		public const int B = 0;
		public const int C = 1;
		public const int E = 2;
		public const int BC = 3;
		public const int BE = 4;

		public const int NPN = 0;
		public const int GAIN = 1;
		public const int GAIN_INV = 2;
		public const int MAX_POW = 3;

		private const double LEAKAGE = 1e-13;
		private const double VT = 0.025865;
		private const double R_GAIN = 0.5;
		private const double R_GAIN_INV = 1.0 / R_GAIN;
		private const double G_MIN = LEAKAGE / 100.0;
		private const double VD_COEF = 1.0 / VT;

		private static double V_CRIT = VT * Math.Log(VT / (Math.Sqrt(2) * LEAKAGE));

		public override int TermCount { get { return 3; } }

		public override double VoltageDiff { get { return V[C] - V[E]; } }

		protected override void DoIteration() {
			double vbc, vbe;
			double bb, bc, be;
			double cb, cc, ce;
			double eb, ec, ee;

			bb = Para[NPN];

			/* check diff voltage */
			{
				vbc = V[B] - V[C];
				vbe = V[B] - V[E];
				bc = V[BC];
				be = V[BE];
				if (0.001 < Math.Abs(vbc - bc) || 0.001 < Math.Abs(vbe - be)) {
					CONVERGED = false;
				}
				vbc *= bb;
				vbe *= bb;
				bc *= bb;
				be *= bb;
				vbc = LIMIT_STEP(vbc, bc);
				vbe = LIMIT_STEP(vbe, be);
				V[BC] = vbc * bb;
				V[BE] = vbe * bb;
			}

			/* update current */
			{
				cc = bb * VD_COEF;
				bc = vbc * cc;
				be = vbe * cc;
				bc = Math.Exp(bc);
				be = Math.Exp(be);
				cb = bc - 1;
				eb = be - 1;
				bb *= LEAKAGE;
				cc = eb;
				cc -= cb * R_GAIN_INV;
				cc *= bb;
				ce = cb;
				ce -= eb * Para[GAIN_INV];
				ce *= bb;
				I[B] = -cc - ce;
				I[C] = cc;
				I[E] = ce;
			}

			/* set conductance */
			{
				bb = LEAKAGE * VD_COEF;
				ec = bc * bb;
				ce = be * bb;
				cc = -ec * R_GAIN_INV;
				ee = -ce;
				ce *= Para[GAIN];
				///* add minimum conductance (bb) between b,e and b,c
				///* To prevent a possible singular matrix,
				///* put a tiny conductance in parallel with each P-N junction. */
				//bb = G_MIN;
				//if (ITER_COUNT > 100) {
				//	bb = Math.Exp(-9 * Math.Log(10) * (1 - ITER_COUNT / 3000.0));
				//	if (bb > 0.1) {
				//		bb = 0.1;
				//	}
				//}
				//cc -= bb;
				//ee -= bb;
				cc -= G_MIN;
				ee -= G_MIN;
				bc = cc + ec;
				be = ee + ce;
				bb = bc + be;
				cb = cc + ce;
				eb = ec + ee;
			}

			/***** set matrix *****/
			var niB = NODE_INFOS[Nodes[B] - 1];
			var niC = NODE_INFOS[Nodes[C] - 1];
			var niE = NODE_INFOS[Nodes[E] - 1];
			var rowB = niB.Row;
			var rowC = niC.Row;
			var rowE = niE.Row;
			MATRIX[rowB, niB.Col] -= bb * niB.IsVariable;
			MATRIX[rowB, niC.Col] += bc * niC.IsVariable;
			MATRIX[rowB, niE.Col] += be * niE.IsVariable;
			MATRIX[rowC, niB.Col] += cb * niB.IsVariable;
			MATRIX[rowC, niC.Col] -= cc * niC.IsVariable;
			MATRIX[rowC, niE.Col] -= ce * niE.IsVariable;
			MATRIX[rowE, niB.Col] += eb * niB.IsVariable;
			MATRIX[rowE, niC.Col] -= ec * niC.IsVariable;
			MATRIX[rowE, niE.Col] -= ee * niE.IsVariable;

			/***** set right side *****/
			bb *= niB.Value;
			cb *= -niB.Value;
			eb *= -niB.Value;
			bb -= bc * niC.Value;
			cb += cc * niC.Value;
			eb += ec * niC.Value;
			bb -= be * niE.Value;
			cb += ce * niE.Value;
			eb += ee * niE.Value;
			bb -= bc * vbc;
			cb += cc * vbc;
			eb += ec * vbc;
			bb -= be * vbe;
			cb += ce * vbe;
			eb += ee * vbe;
			bb -= I[B];
			cb -= I[C];
			eb -= I[E];
			RIGHTSIDE[rowB] += bb;
			RIGHTSIDE[rowC] += cb;
			RIGHTSIDE[rowE] += eb;
		}

		protected override void FinishIteration() {
			var pc = (V[C] - V[E]) * I[C];
			if (pc > Para[MAX_POW] || pc < -Para[MAX_POW]) {
				Broken = true;
				CircuitState.Stopped = true;
			}
		}

		protected override double GetCurrent(int n) { return I[n]; }

		private static double LIMIT_STEP(double newVal, double oldVal) {
			if (newVal > V_CRIT && Math.Abs(newVal - oldVal) > (VT + VT)) {
				if (oldVal > 0) {
					var arg = 1 + (newVal - oldVal) / VT;
					if (arg > 0) {
						newVal = oldVal + VT * Math.Log(arg);
					} else {
						newVal = V_CRIT;
					}
				} else {
					newVal = VT * Math.Log(newVal / VT);
				}
				CONVERGED = false;
			}
			return newVal;
		}
	}
}
