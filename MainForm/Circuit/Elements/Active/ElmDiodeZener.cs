namespace Circuit.Elements.Active {
	class ElmDiodeZener : BaseElement {
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
			var v_diff = V[0] - V[mDiodeEndNode];
			if (0.001 < Math.Abs(v_diff - mLastVoltDiff)) {
				CONVERGED = false;
			}

			/* limit Vdiff */
			var v_old = mLastVoltDiff;
			mLastVoltDiff = v_diff;
			{
				/* check new Vdiff; has current changed by factor of e^2? */
				if (v_diff > mVCrit && Math.Abs(v_diff - v_old) > (VScale + VScale)) {
					if (v_old > 0) {
						var arg = 1 + (v_diff - v_old) / VScale;
						if (arg > 0) {
							/* adjust Vdiff so that the current is the same
                             * as in linearized model from previous iteration.
                             * current at Vdiff = old current * arg */
							v_diff = v_old + VScale * Math.Log(arg);
						} else {
							v_diff = mVCrit;
						}
					} else {
						/* adjust Vdiff so that the current is the same
                         * as in linearized model from previous iteration.
                         * (1/vscale = slope of load line) */
						v_diff = VScale * Math.Log(v_diff / VScale);
					}
					CONVERGED = false;
				} else if (v_diff < 0 && mVzOffset != 0) {
					/* for Zener breakdown, use the same logic but translate the values,
                     * and replace the normal values with the Zener-specific ones to
                     * account for the steeper exponential of our Zener breakdown curve. */
					v_diff = -v_diff - mVzOffset;
					v_old = -v_old - mVzOffset;
					if (v_diff > mVzCrit && Math.Abs(v_diff - v_old) > (VTH + VTH)) {
						if (v_old > 0) {
							var arg = 1 + (v_diff - v_old) / VTH;
							if (arg > 0) {
								v_diff = v_old + VTH * Math.Log(arg);
							} else {
								v_diff = mVzCrit;
							}
						} else {
							v_diff = VTH * Math.Log(v_diff / VTH);
						}
						CONVERGED = false;
					}
					v_diff = -(v_diff + mVzOffset);
				}
			}

			{
				/* To prevent a possible singular matrix or other numeric issues, put a tiny conductance
                 * in parallel with each P-N junction. */
				var g_min = Leakage * 0.01;
				if (ITER_COUNT > 100) {
					/* if we have trouble converging, put a conductance in parallel with the diode.
                     * Gradually increase the conductance value for each iteration. */
					g_min = Math.Exp(-9 * Math.Log(10) * (1 - ITER_COUNT / 3000.0));
					if (g_min > 0.1) {
						g_min = 0.1;
					}
				}
				double geq;
				double nc;
				if (v_diff >= 0 || VZener == 0) {
					/* regular diode or forward-biased zener */
					var eval = Math.Exp(v_diff * VdCoef);
					geq = VdCoef * Leakage * eval + g_min;
					nc = (eval - 1) * Leakage - geq * v_diff;
				} else {
					/* Zener diode */
					/* For reverse-biased Zener diodes, mimic the Zener breakdown curve with an
                     * exponential similar to the ideal Shockley curve. (The real breakdown curve
                     * isn't a simple exponential, but this approximation should be OK.) */
					/* 
                     * I(Vd) = Is * (exp[Vd*C] - exp[(-Vd-Vz)*Cz] - 1 )
                     *
                     * geq is I'(Vd)
                     * nc is I(Vd) + I'(Vd)*(-Vd)
                     */
					geq = Leakage * (
						VdCoef * Math.Exp(v_diff * VdCoef)
						+ VZ_COEF * Math.Exp((-v_diff - mVzOffset) * VZ_COEF)
					) + g_min;
					nc = Leakage * (
						Math.Exp(v_diff * VdCoef)
						- Math.Exp((-v_diff - mVzOffset) * VZ_COEF)
						- 1
					) + geq * (-v_diff);
				}
				UpdateConductance(mNodes0, mNodes1, geq);
				UpdateCurrentSource(mNodes0, mNodes1, nc);
			}
		}

		protected override void FinishIteration() {
			if (Math.Abs(I[0]) > 1e12) {
				CircuitState.Stopped = true;
			}
		}

		public override void SetVoltage(int n, double v) {
			V[n] = v;
			var voltdiff = V[0] - V[mDiodeEndNode];
			if (voltdiff >= 0 || VZener == 0) {
				I[0] = Leakage * (Math.Exp(voltdiff * VdCoef) - 1);
			} else {
				I[0] = Leakage * (
					Math.Exp(voltdiff * VdCoef)
					- Math.Exp((-voltdiff - mVzOffset) * VZ_COEF)
					- 1
				);
			}
		}
	}
}
