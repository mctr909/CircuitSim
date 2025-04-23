namespace Circuit.Elements.Active {
	class ElmDiode : BaseElement {
		public const double VTH = 0.025865;
		public const double VZ_COEF = 1 / VTH;

		public const int LEAKAGE = 0;
		public const int V_SCALE = 1;
		public const int V_CRIT = 2;
		public const int VD_COEF = 3;

		public const int V_ZENER = 4;
		public const int VZ_CRIT = 5;
		public const int VZ_OFFSET = 6;

		public const int CAPACITANCE = 5;
		public const int RESISTANCE = 6;
		public const int FW_DROP = 7;

		public const int VD = 2;
		public const int VD_CAP = 3;
		public const int VS_CAP = 4;

		public const int CUR_CAP = 1;

		protected override void DoIteration() {
			/* limit Vdiff */
			var vdNew = V[0] - V[1];
			var vdOld = V[VD];
			if (0.001 < Math.Abs(vdNew - vdOld)) {
				CONVERGED = false;
			}
			V[VD] = vdNew;

			/* check new Vdiff; has current changed by factor of e^2? */
			var vScale = Para[V_SCALE];
			if (vdNew > Para[V_CRIT] && Math.Abs(vdNew - vdOld) > (vScale + vScale)) {
				if (vdOld > 0) {
					var arg = 1 + (vdNew - vdOld) / vScale;
					if (arg > 0) {
						/* adjust Vdiff so that the current is the same
						 * as in linearized model from previous iteration.
						 * current at Vdiff = old current * arg */
						vdNew = vdOld + vScale * Math.Log(arg);
					} else {
						vdNew = Para[V_CRIT];
					}
				} else {
					/* adjust Vdiff so that the current is the same
					 * as in linearized model from previous iteration.
					 * (1/vscale = slope of load line) */
					vdNew = vScale * Math.Log(vdNew / vScale);
				}
				CONVERGED = false;
			}

			/* To prevent a possible singular matrix or other numeric issues, put a tiny conductance
			 * in parallel with each P-N junction. */
			var leakage = Para[LEAKAGE];
			var gMin = leakage * 0.01;
			//if (ITER_COUNT > 100) {
			//	/* if we have trouble converging, put a conductance in parallel with the diode.
			//   * Gradually increase the conductance value for each iteration. */
			//	gMin = Math.Exp(-9 * Math.Log(10) * (1 - ITER_COUNT / 3000.0));
			//	if (gMin > 0.1) {
			//		gMin = 0.1;
			//	}
			//}
			/* regular diode */
			var vdCoef = Para[VD_COEF];
			var vdExp = Math.Exp(vdNew * vdCoef);
			var geq = vdCoef * vdExp * leakage + gMin;
			var nc = (vdExp - 1) * leakage - geq * vdNew;
			UpdateConductance(Nodes[0], Nodes[1], geq);
			UpdateCurrentSource(Nodes[0], Nodes[1], nc);
		}

		protected override void FinishIteration() {
			if (Math.Abs(I[0]) > 1e12) {
				CircuitState.Stopped = true;
			}
		}

		public override void SetVoltage(int n, double v) {
			V[n] = v;
			var vd = V[0] - V[1];
			I[0] = Para[LEAKAGE] * (Math.Exp(vd * Para[VD_COEF]) - 1);
		}
	}
}
