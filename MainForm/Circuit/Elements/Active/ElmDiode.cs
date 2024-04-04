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
			alloc_nodes();
		}

		#region [method(Analyze)]
		public override void reset() {
			mLastVoltDiff = 0;
			volts[0] = volts[1] = 0;
			if (mHasResistance) {
				volts[2] = 0;
			}
		}

		public override void stamp() {
			if (mHasResistance) {
				/* create diode from node 0 to internal node */
				mNodes0 = node_index[0];
				mNodes1 = node_index[2];
				CircuitElement.NodeInfo[mNodes0 - 1].left_changes = true;
				CircuitElement.NodeInfo[mNodes1 - 1].left_changes = true;
				/* create resistor from internal node to node 1 */
				var r0 = 1.0 / SeriesResistance;
				CircuitElement.Matrix[node_index[1] - 1, node_index[1] - 1] += r0;
				CircuitElement.Matrix[node_index[2] - 1, node_index[2] - 1] += r0;
				CircuitElement.Matrix[node_index[1] - 1, node_index[2] - 1] -= r0;
				CircuitElement.Matrix[node_index[2] - 1, node_index[1] - 1] -= r0;
			} else {
				/* don't need any internal nodes if no series resistance */
				mNodes0 = node_index[0];
				mNodes1 = node_index[1];
				CircuitElement.NodeInfo[mNodes0 - 1].left_changes = true;
				CircuitElement.NodeInfo[mNodes1 - 1].left_changes = true;
			}
		}
		#endregion

		#region [method(Circuit)]
		public override void do_iteration() {
			var v_diff = volts[0] - volts[mDiodeEndNode];
			if (0.001 < Math.Abs(v_diff - mLastVoltDiff)) {
				CircuitElement.Converged = false;
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
					CircuitElement.Converged = false;
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
						CircuitElement.Converged = false;
					}
					v_diff = -(v_diff + mVzOffset);
				}
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
				if (v_diff >= 0 || VZener == 0) {
					/* regular diode or forward-biased zener */
					var eval = Math.Exp(v_diff * VdCoef);
					geq = VdCoef * Leakage * eval + gmin;
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
					) + gmin;
					nc = Leakage * (
						Math.Exp(v_diff * VdCoef)
						- Math.Exp((-v_diff - mVzOffset) * VZ_COEF)
						- 1
					) + geq * (-v_diff);
				}
				/***** Set matrix *****/
				var n0 = CircuitElement.NodeInfo[mNodes0 - 1];
				var n1 = CircuitElement.NodeInfo[mNodes1 - 1];
				if (n0.is_const) {
					CircuitElement.RightSide[n0.ROW] -= geq * n0.value;
					CircuitElement.RightSide[n1.ROW] += geq * n0.value;
				} else {
					CircuitElement.Matrix[n0.ROW, n0.COL] += geq;
					CircuitElement.Matrix[n1.ROW, n0.COL] -= geq;
				}
				if (n1.is_const) {
					CircuitElement.RightSide[n1.ROW] -= geq * n1.value;
					CircuitElement.RightSide[n0.ROW] += geq * n1.value;
				} else {
					CircuitElement.Matrix[n1.ROW, n1.COL] += geq;
					CircuitElement.Matrix[n0.ROW, n1.COL] -= geq;
				}
				CircuitElement.RightSide[n0.ROW] -= nc;
				CircuitElement.RightSide[n1.ROW] += nc;
			}
		}

		public override void finish_iteration() {
			if (Math.Abs(current) > 1e12) {
				CircuitElement.Stopped = true;
			}
		}

		public override void set_voltage(int n, double c) {
			volts[n] = c;
			var voltdiff = volts[0] - volts[mDiodeEndNode];
			if (voltdiff >= 0 || VZener == 0) {
				current = Leakage * (Math.Exp(voltdiff * VdCoef) - 1);
			} else {
				current = Leakage * (
					Math.Exp(voltdiff * VdCoef)
					- Math.Exp((-voltdiff - mVzOffset) * VZ_COEF)
					- 1
				);
			}
		}
		#endregion
	}
}
