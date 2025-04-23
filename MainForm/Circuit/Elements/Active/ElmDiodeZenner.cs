namespace Circuit.Elements.Active {
	class ElmDiodeZenner : ElmDiode {
		protected override void DoIteration() {
			/* limit Vdiff */
			var vdNew = V[0] - V[1];
			var vdOld = V[VD];
			if (0.001 < Math.Abs(vdNew - vdOld)) {
				CONVERGED = false;
			}
			V[VD] = vdNew;

			var vzOffset = Para[VZ_OFFSET];

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
			} else if (vdNew < 0 && vzOffset != 0) {
				/* for Zener breakdown, use the same logic but translate the values,
				 * and replace the normal values with the Zener-specific ones to
				 * account for the steeper exponential of our Zener breakdown curve. */
				vdNew = -(vdNew + vzOffset);
				vdOld = -(vdOld + vzOffset);
				if (vdNew > Para[VZ_CRIT] && Math.Abs(vdNew - vdOld) > (VTH + VTH)) {
					if (vdOld > 0) {
						var arg = 1 + (vdNew - vdOld) / VTH;
						if (arg > 0) {
							vdNew = vdOld + VTH * Math.Log(arg);
						} else {
							vdNew = Para[VZ_CRIT];
						}
					} else {
						vdNew = VTH * Math.Log(vdNew / VTH);
					}
					CONVERGED = false;
				}
				vdNew = -(vdNew + vzOffset);
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
			/* Zener diode */
			// For reverse-biased Zener diodes, mimic the Zener breakdown curve with an
			// exponential similar to the ideal Shockley curve. (The real breakdown curve
			// isn't a simple exponential, but this approximation should be OK.)
			//
			// I(Vd) = Is * (exp[Vd*C] - exp[(-Vd-Vz)*Cz] - 1 )
			// geq is I'(Vd)
			// nc is I(Vd) + I'(Vd)*(-Vd)
			var vdCoef = Para[VD_COEF];
			var vdExp = Math.Exp(vdNew * vdCoef);
			var vdzExp = Math.Exp(-(vdNew + vzOffset) * VZ_COEF);
			var geq = (vdCoef * vdExp + VZ_COEF * vdzExp) * leakage + gMin;
			var nc = (vdExp - vdzExp - 1) * leakage - geq * vdNew;
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
			var vdExp = Math.Exp(vd * Para[VD_COEF]) - 1;
			if (vd < 0) {
				vdExp -= Math.Exp(-(vd + Para[VZ_OFFSET]) * VZ_COEF);
			}
			I[0] = Para[LEAKAGE] * vdExp;
		}
	}
}
