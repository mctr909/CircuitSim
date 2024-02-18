namespace Circuit.Elements.Active {
	class ElmDiode : BaseElement {
		/* Electron thermal voltage at SPICE's default temperature of 27 C (300.15 K): */
		const double VT = 0.025865;

		/* The Zener breakdown curve is represented by a steeper exponential, one like the ideal
         * Shockley curve, but flipped and translated. This curve removes the moderating influence
         * of emcoef, replacing vscale and vdcoef with vt and vzcoef.
         * vzcoef is the multiplicative equivalent of dividing by vt (for speed). */
		const double VZ_COEF = 1 / VT;

		public DiodeModel Model;

		/// <summary>
		/// User-specified diode parameters for Zener voltage.
		/// </summary>
		public double Zvoltage;
		public double FwDrop;

		bool mHasResistance;
		int mNodes0;
		int mNodes1;
		int mDiodeEndNode;
		double mLastVoltDiff;
		double mSeriesResistance;

		/// <summary>
		/// The diode's "scale voltage", the voltage increase which will raise current by a factor of e.
		/// </summary>
		double mVscale;

		/// <summary>
		/// The multiplicative equivalent of dividing by vscale (for speed).
		/// </summary>
		double mVdCoef;

		/// <summary>
		/// The diode current's scale factor, calculated from the user-specified forward voltage drop.
		/// </summary>
		double mLeakage;

		/// <summary>
		/// Voltage offset for Zener breakdown exponential, calculated from user-specified Zener voltage.
		/// </summary>
		double mZoffset;

		/// <summary>
		/// Critical voltages for limiting the normal diode.
		/// </summary>
		double mVcrit;

		/// <summary>
		/// Critical voltages for limiting Zener breakdown exponentials.
		/// </summary>
		double mVzCrit;

		public ElmDiode() { }

		public override int TermCount { get { return 2; } }

		public override int InternalNodeCount { get { return mHasResistance ? 1 : 0; } }

		public void Setup(string modelName) {
			Model = DiodeModel.GetModelWithNameOrCopy(modelName, Model);
			Zvoltage = Model.BreakdownVoltage;
			FwDrop = Model.FwDrop;
			mLeakage = Model.SaturationCurrent;
			mVscale = Model.VScale;
			mVdCoef = Model.VdCoef;
			mSeriesResistance = Model.SeriesResistance;
			/* critical voltage for limiting; current is vscale/sqrt(2) at this voltage */
			mVcrit = mVscale * Math.Log(mVscale / (Math.Sqrt(2) * mLeakage));
			/* translated, *positive* critical voltage for limiting in Zener breakdown region;
             * limitstep() uses this with translated voltages in an analogous fashion to vcrit. */
			mVzCrit = VT * Math.Log(VT / (Math.Sqrt(2) * mLeakage));
			if (Zvoltage == 0) {
				mZoffset = 0;
			} else {
				/* calculate offset which will give us 5mA at zvoltage */
				double i = -0.005;
				mZoffset = Zvoltage - Math.Log(-(1 + i / mLeakage)) / VZ_COEF;
			}
			mHasResistance = 0 < mSeriesResistance;
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
				var r0 = 1.0 / mSeriesResistance;
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
				if (v_new > mVcrit && Math.Abs(v_new - v_old) > (mVscale + mVscale)) {
					if (v_old > 0) {
						var arg = 1 + (v_new - v_old) / mVscale;
						if (arg > 0) {
							/* adjust vnew so that the current is the same
                             * as in linearized model from previous iteration.
                             * current at vnew = old current * arg */
							v_new = v_old + mVscale * Math.Log(arg);
						} else {
							v_new = mVcrit;
						}
					} else {
						/* adjust vnew so that the current is the same
                         * as in linearized model from previous iteration.
                         * (1/vscale = slope of load line) */
						v_new = mVscale * Math.Log(v_new / mVscale);
					}
					CircuitElement.Converged = false;
				} else if (v_new < 0 && mZoffset != 0) {
					/* for Zener breakdown, use the same logic but translate the values,
                     * and replace the normal values with the Zener-specific ones to
                     * account for the steeper exponential of our Zener breakdown curve. */
					v_new = -v_new - mZoffset;
					v_old = -v_old - mZoffset;
					if (v_new > mVzCrit && Math.Abs(v_new - v_old) > (VT + VT)) {
						if (v_old > 0) {
							var arg = 1 + (v_new - v_old) / VT;
							if (arg > 0) {
								v_new = v_old + VT * Math.Log(arg);
							} else {
								v_new = mVzCrit;
							}
						} else {
							v_new = VT * Math.Log(v_new / VT);
						}
						CircuitElement.Converged = false;
					}
					v_new = -(v_new + mZoffset);
				}
				voltdiff = v_new;
				mLastVoltDiff = voltdiff;
			}

			{
				/* To prevent a possible singular matrix or other numeric issues, put a tiny conductance
                 * in parallel with each P-N junction. */
				var gmin = mLeakage * 0.01;
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
				if (voltdiff >= 0 || Zvoltage == 0) {
					/* regular diode or forward-biased zener */
					var eval = Math.Exp(voltdiff * mVdCoef);
					geq = mVdCoef * mLeakage * eval + gmin;
					nc = (eval - 1) * mLeakage - geq * voltdiff;
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
					geq = mLeakage * (
						mVdCoef * Math.Exp(voltdiff * mVdCoef)
						+ VZ_COEF * Math.Exp((-voltdiff - mZoffset) * VZ_COEF)
					) + gmin;
					nc = mLeakage * (
						Math.Exp(voltdiff * mVdCoef)
						- Math.Exp((-voltdiff - mZoffset) * VZ_COEF)
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

		public override void SetVoltage(int n, double c) {
			Volts[n] = c;
			var voltdiff = Volts[0] - Volts[mDiodeEndNode];
			if (voltdiff >= 0 || Zvoltage == 0) {
				Current = mLeakage * (Math.Exp(voltdiff * mVdCoef) - 1);
			} else {
				Current = mLeakage * (
					Math.Exp(voltdiff * mVdCoef)
					- Math.Exp((-voltdiff - mZoffset) * VZ_COEF)
					- 1
				);
			}
		}

		public override void IterationFinished() {
			if (Math.Abs(Current) > 1e12) {
				CircuitElement.Stop(this);
			}
		}
	}
}
