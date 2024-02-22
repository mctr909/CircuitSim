namespace Circuit.Elements.Active {
	class ElmDiode : BaseElement {
		const double VTH = 0.025865;
		const double VZ_COEF = 1 / VTH;

		public DiodeModel Model;

		public double FwDrop;
		public double VZener;
		public double Leakage;
		public double VScale;
		public double VdCoef;
		public double SeriesResistance;

		double mVCrit;
		double mVzCrit;
		double mVzOffset;
		double mLastVoltDiff;

		bool mHasResistance;
		int mNodes0;
		int mNodes1;
		int mDiodeEndNode;

		public override int TermCount { get { return 2; } }

		public override int InternalNodeCount { get { return mHasResistance ? 1 : 0; } }

		public ElmDiode() { }

		public void Setup() {
			/* critical voltage for limiting; current is vscale/sqrt(2) at this voltage */
			mVCrit = VScale * Math.Log(VScale / (Math.Sqrt(2) * Leakage));
			/* translated, *positive* critical voltage for limiting in Zener breakdown region;
             * limitstep() uses this with translated voltages in an analogous fashion to vcrit. */
			mVzCrit = VTH * Math.Log(VTH / (Math.Sqrt(2) * Leakage));
			if (VZener == 0) {
				mVzOffset = 0;
			} else {
				/* calculate offset which will give us 5mA at zvoltage */
				double i = -0.005;
				mVzOffset = VZener - Math.Log(-(1 + i / Leakage)) / VZ_COEF;
			}
			mHasResistance = 0 < SeriesResistance;
			mDiodeEndNode = mHasResistance ? 2 : 1;
			AllocNodes();
		}

		public override void Reset() {
			mLastVoltDiff = 0;
			Volts[0] = Volts[1] = 0;
			if (mHasResistance) {
				Volts[2] = 0;
			}
		}

		public override void Stamp() {
			if (mHasResistance) {
				/* create diode from node 0 to internal node */
				mNodes0 = Nodes[0];
				mNodes1 = Nodes[2];
				CircuitElement.RowInfo[mNodes0 - 1].LeftChanges = true;
				CircuitElement.RowInfo[mNodes1 - 1].LeftChanges = true;
				/* create resistor from internal node to node 1 */
				var r0 = 1.0 / SeriesResistance;
				CircuitElement.Matrix[Nodes[1] - 1, Nodes[1] - 1] += r0;
				CircuitElement.Matrix[Nodes[2] - 1, Nodes[2] - 1] += r0;
				CircuitElement.Matrix[Nodes[1] - 1, Nodes[2] - 1] -= r0;
				CircuitElement.Matrix[Nodes[2] - 1, Nodes[1] - 1] -= r0;
			} else {
				/* don't need any internal nodes if no series resistance */
				mNodes0 = Nodes[0];
				mNodes1 = Nodes[1];
				CircuitElement.RowInfo[mNodes0 - 1].LeftChanges = true;
				CircuitElement.RowInfo[mNodes1 - 1].LeftChanges = true;
			}
		}

		public override void DoIteration() {
			var voltdiff = Volts[0] - Volts[mDiodeEndNode];
			if (0.001 < Math.Abs(voltdiff - mLastVoltDiff)) {
				CircuitElement.Converged = false;
			}

			{
				var v_new = voltdiff;
				var v_old = mLastVoltDiff;
				/* check new voltage; has current changed by factor of e^2? */
				if (v_new > mVCrit && Math.Abs(v_new - v_old) > (VScale + VScale)) {
					if (v_old > 0) {
						var arg = 1 + (v_new - v_old) / VScale;
						if (arg > 0) {
							/* adjust vnew so that the current is the same
                             * as in linearized model from previous iteration.
                             * current at vnew = old current * arg */
							v_new = v_old + VScale * Math.Log(arg);
						} else {
							v_new = mVCrit;
						}
					} else {
						/* adjust vnew so that the current is the same
                         * as in linearized model from previous iteration.
                         * (1/vscale = slope of load line) */
						v_new = VScale * Math.Log(v_new / VScale);
					}
					CircuitElement.Converged = false;
				} else if (v_new < 0 && mVzOffset != 0) {
					/* for Zener breakdown, use the same logic but translate the values,
                     * and replace the normal values with the Zener-specific ones to
                     * account for the steeper exponential of our Zener breakdown curve. */
					v_new = -v_new - mVzOffset;
					v_old = -v_old - mVzOffset;
					if (v_new > mVzCrit && Math.Abs(v_new - v_old) > (VTH + VTH)) {
						if (v_old > 0) {
							var arg = 1 + (v_new - v_old) / VTH;
							if (arg > 0) {
								v_new = v_old + VTH * Math.Log(arg);
							} else {
								v_new = mVzCrit;
							}
						} else {
							v_new = VTH * Math.Log(v_new / VTH);
						}
						CircuitElement.Converged = false;
					}
					v_new = -(v_new + mVzOffset);
				}
				voltdiff = v_new;
				mLastVoltDiff = voltdiff;
			}

			{
				/* To prevent a possible singular matrix or other numeric issues, put a tiny conductance
                 * in parallel with each P-N junction. */
				var gmin = Leakage * 0.01;
				if (CircuitElement.SubIterations > 100) {
					/* if we have trouble converging, put a conductance in parallel with the diode.
                     * Gradually increase the conductance value for each iteration. */
					gmin = Math.Exp(-9 * Math.Log(10) * (1 - CircuitElement.SubIterations / 3000.0));
					if (0.1 < gmin) {
						gmin = 0.1;
					}
				}
				double geq;
				double nc;
				if (voltdiff >= 0 || VZener == 0) {
					/* regular diode or forward-biased zener */
					var eval = Math.Exp(voltdiff * VdCoef);
					geq = VdCoef * Leakage * eval + gmin;
					nc = (eval - 1) * Leakage - geq * voltdiff;
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
						VdCoef * Math.Exp(voltdiff * VdCoef)
						+ VZ_COEF * Math.Exp((-voltdiff - mVzOffset) * VZ_COEF)
					) + gmin;
					nc = Leakage * (
						Math.Exp(voltdiff * VdCoef)
						- Math.Exp((-voltdiff - mVzOffset) * VZ_COEF)
						- 1
					) + geq * (-voltdiff);
				}
				var row = CircuitElement.RowInfo[mNodes0 - 1].MapRow;
				var ri = CircuitElement.RowInfo[mNodes0 - 1];
				if (ri.IsConst) {
					CircuitElement.RightSide[row] -= geq * ri.Value;
				} else {
					CircuitElement.Matrix[row, ri.MapCol] += geq;
				}
				row = CircuitElement.RowInfo[mNodes1 - 1].MapRow;
				ri = CircuitElement.RowInfo[mNodes1 - 1];
				if (ri.IsConst) {
					CircuitElement.RightSide[row] -= geq * ri.Value;
				} else {
					CircuitElement.Matrix[row, ri.MapCol] += geq;
				}
				row = CircuitElement.RowInfo[mNodes0 - 1].MapRow;
				ri = CircuitElement.RowInfo[mNodes1 - 1];
				if (ri.IsConst) {
					CircuitElement.RightSide[row] += geq * ri.Value;
				} else {
					CircuitElement.Matrix[row, ri.MapCol] -= geq;
				}
				row = CircuitElement.RowInfo[mNodes1 - 1].MapRow;
				ri = CircuitElement.RowInfo[mNodes0 - 1];
				if (ri.IsConst) {
					CircuitElement.RightSide[row] += geq * ri.Value;
				} else {
					CircuitElement.Matrix[row, ri.MapCol] -= geq;
				}
				CircuitElement.RightSide[CircuitElement.RowInfo[mNodes0 - 1].MapRow] -= nc;
				CircuitElement.RightSide[CircuitElement.RowInfo[mNodes1 - 1].MapRow] += nc;
			}
		}

		public override void FinishIteration() {
			if (Math.Abs(Current) > 1e12) {
				CircuitElement.Stop(this);
			}
		}

		public override void SetVoltage(int n, double c) {
			Volts[n] = c;
			var voltdiff = Volts[0] - Volts[mDiodeEndNode];
			if (voltdiff >= 0 || VZener == 0) {
				Current = Leakage * (Math.Exp(voltdiff * VdCoef) - 1);
			} else {
				Current = Leakage * (
					Math.Exp(voltdiff * VdCoef)
					- Math.Exp((-voltdiff - mVzOffset) * VZ_COEF)
					- 1
				);
			}
		}
	}
}
