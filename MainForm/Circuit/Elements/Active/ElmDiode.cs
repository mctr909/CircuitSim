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
				CircuitElement.row_info[mNodes0 - 1].left_changes = true;
				CircuitElement.row_info[mNodes1 - 1].left_changes = true;
				/* create resistor from internal node to node 1 */
				var r0 = 1.0 / SeriesResistance;
				CircuitElement.matrix[node_index[1] - 1, node_index[1] - 1] += r0;
				CircuitElement.matrix[node_index[2] - 1, node_index[2] - 1] += r0;
				CircuitElement.matrix[node_index[1] - 1, node_index[2] - 1] -= r0;
				CircuitElement.matrix[node_index[2] - 1, node_index[1] - 1] -= r0;
			} else {
				/* don't need any internal nodes if no series resistance */
				mNodes0 = node_index[0];
				mNodes1 = node_index[1];
				CircuitElement.row_info[mNodes0 - 1].left_changes = true;
				CircuitElement.row_info[mNodes1 - 1].left_changes = true;
			}
		}
		#endregion

		#region [method(Circuit)]
		public override void do_iteration() {
			var voltdiff = volts[0] - volts[mDiodeEndNode];
			if (0.001 < Math.Abs(voltdiff - mLastVoltDiff)) {
				CircuitElement.converged = false;
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
					CircuitElement.converged = false;
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
						CircuitElement.converged = false;
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
				if (CircuitElement.sub_iterations > 100) {
					/* if we have trouble converging, put a conductance in parallel with the diode.
                     * Gradually increase the conductance value for each iteration. */
					gmin = Math.Exp(-9 * Math.Log(10) * (1 - CircuitElement.sub_iterations / 3000.0));
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
				var row = CircuitElement.row_info[mNodes0 - 1].row;
				var ri = CircuitElement.row_info[mNodes0 - 1];
				if (ri.is_const) {
					CircuitElement.right_side[row] -= geq * ri.value;
				} else {
					CircuitElement.matrix[row, ri.col] += geq;
				}
				row = CircuitElement.row_info[mNodes1 - 1].row;
				ri = CircuitElement.row_info[mNodes1 - 1];
				if (ri.is_const) {
					CircuitElement.right_side[row] -= geq * ri.value;
				} else {
					CircuitElement.matrix[row, ri.col] += geq;
				}
				row = CircuitElement.row_info[mNodes0 - 1].row;
				ri = CircuitElement.row_info[mNodes1 - 1];
				if (ri.is_const) {
					CircuitElement.right_side[row] += geq * ri.value;
				} else {
					CircuitElement.matrix[row, ri.col] -= geq;
				}
				row = CircuitElement.row_info[mNodes1 - 1].row;
				ri = CircuitElement.row_info[mNodes0 - 1];
				if (ri.is_const) {
					CircuitElement.right_side[row] += geq * ri.value;
				} else {
					CircuitElement.matrix[row, ri.col] -= geq;
				}
				CircuitElement.right_side[CircuitElement.row_info[mNodes0 - 1].row] -= nc;
				CircuitElement.right_side[CircuitElement.row_info[mNodes1 - 1].row] += nc;
			}
		}

		public override void finish_iteration() {
			if (Math.Abs(current) > 1e12) {
				CircuitElement.stopped = true;
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
