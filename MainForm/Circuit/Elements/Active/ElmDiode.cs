namespace Circuit.Elements.Active {
	class ElmDiode : BaseElement {
		public const double VTH = 0.025865;
		public const double VZ_COEF = 1 / VTH;

		public double FwDrop;
		public double VZener;
		public double Leakage;
		public double VScale;
		public double VdCoef;
		public double SeriesResistance;

		public double mVCrit;
		public double mVzCrit;
		public double mVzOffset;
		public double mLastVoltDiff;

		public int mNodes0;
		public int mNodes1;
		public int mDiodeEndNode;

		protected override void DoIteration() {
			/* limit Vdiff */
			var vdNew = V[0] - V[mDiodeEndNode];
			var vdOld = mLastVoltDiff;
			mLastVoltDiff = vdNew;
			if (0.001 < Math.Abs(vdNew - vdOld)) {
				CONVERGED = false;
			}

			/* check new Vdiff; has current changed by factor of e^2? */
			if (vdNew > mVCrit && Math.Abs(vdNew - vdOld) > (VScale + VScale)) {
				if (vdOld > 0) {
					var arg = 1 + (vdNew - vdOld) / VScale;
					if (arg > 0) {
						/* adjust Vdiff so that the current is the same
						 * as in linearized model from previous iteration.
						 * current at Vdiff = old current * arg */
						vdNew = vdOld + VScale * Math.Log(arg);
					} else {
						vdNew = mVCrit;
					}
				} else {
					/* adjust Vdiff so that the current is the same
					 * as in linearized model from previous iteration.
					 * (1/vscale = slope of load line) */
					vdNew = VScale * Math.Log(vdNew / VScale);
				}
				CONVERGED = false;
			}

			/* To prevent a possible singular matrix or other numeric issues, put a tiny conductance
			 * in parallel with each P-N junction. */
			var gMin = Leakage * 0.01;
			//if (ITER_COUNT > 100) {
			//	/* if we have trouble converging, put a conductance in parallel with the diode.
			//	 * Gradually increase the conductance value for each iteration. */
			//	gMin = Math.Exp(-9 * Math.Log(10) * (1 - ITER_COUNT / 3000.0));
			//	if (gMin > 0.1) {
			//		gMin = 0.1;
			//	}
			//}

			/* regular diode */
			var eval = Math.Exp(vdNew * VdCoef);
			var geq = VdCoef * Leakage * eval + gMin;
			var nc = (eval - 1) * Leakage - geq * vdNew;
			UpdateConductance(mNodes0, mNodes1, geq);
			UpdateCurrentSource(mNodes0, mNodes1, nc);
		}

		protected override void FinishIteration() {
			if (Math.Abs(I[0]) > 1e12) {
				CircuitState.Stopped = true;
			}
		}

		public override void SetVoltage(int n, double v) {
			V[n] = v;
			var vd = V[0] - V[mDiodeEndNode];
			I[0] = Leakage * (Math.Exp(vd * VdCoef) - 1);
		}
	}
}
