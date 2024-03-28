namespace Circuit.Elements.Passive {
	class ElmTransformer : BaseElement {
		public const int PRI_T = 0;
		public const int PRI_B = 2;
		public const int SEC_T = 1;
		public const int SEC_B = 3;

		public double PInductance = 0.01;
		public double Ratio = 1.0;
		public double CouplingCoef = 0.999;
		public int Polarity = 1;

		public double[] Currents = new double[2];

		double[] mA = new double[4];
		double[] mCurSourceValue = new double[2];

		public override int TermCount { get { return 4; } }

		#region [method(Analyze)]
		public override bool has_connection(int n1, int n2) {
			if (ComparePair(n1, n2, 0, 2)) {
				return true;
			}
			if (ComparePair(n1, n2, 1, 3)) {
				return true;
			}
			return false;
		}

		public override void reset() {
			/* need to set current-source values here in case one of the nodes is node 0.  In that case
             * calculateCurrent() may get called (from setNodeVoltage()) when analyzing circuit, before
             * startIteration() gets called */
			Currents[0] = Currents[1] = 0;
			volts[PRI_T] = volts[PRI_B] = 0;
			volts[SEC_T] = volts[SEC_B] = 0;
			mCurSourceValue[0] = mCurSourceValue[1] = 0;
		}

		public override void stamp() {
			/* equations for transformer:
             *   v1 = L1 di1/dt + M  di2/dt
             *   v2 = M  di1/dt + L2 di2/dt
             * we invert that to get:
             *   di1/dt = a1 v1 + a2 v2
             *   di2/dt = a3 v1 + a4 v2
             * integrate di1/dt using trapezoidal approx and we get:
             *   i1(t2) = i1(t1) + dt/2 (i1(t1) + i1(t2))
             *          = i1(t1) + a1 dt/2 v1(t1) + a2 dt/2 v2(t1) +
             *                     a1 dt/2 v1(t2) + a2 dt/2 v2(t2)
             * the norton equivalent of this for i1 is:
             *  a. current source, I = i1(t1) + a1 dt/2 v1(t1) + a2 dt/2 v2(t1)
             *  b. resistor, G = a1 dt/2
             *  c. current source controlled by voltage v2, G = a2 dt/2
             * and for i2:
             *  a. current source, I = i2(t1) + a3 dt/2 v1(t1) + a4 dt/2 v2(t1)
             *  b. resistor, G = a3 dt/2
             *  c. current source controlled by voltage v2, G = a4 dt/2
             *
             * For backward euler,
             *
             *   i1(t2) = i1(t1) + a1 dt v1(t2) + a2 dt v2(t2)
             *
             * So the current source value is just i1(t1) and we use
             * dt instead of dt/2 for the resistor and VCCS.
             *
             * first winding goes from node 0 to 2, second is from 1 to 3 */
			var l1 = PInductance;
			var l2 = PInductance * Ratio * Ratio;
			var m = CouplingCoef * Math.Sqrt(l1 * l2);
			// build inverted matrix
			var deti = 1 / (l1 * l2 - m * m);
			var ts = CircuitElement.delta_time / 2;
			mA[0] = l2 * deti * ts; // we multiply dt/2 into a1..a4 here
			mA[1] = -m * deti * ts;
			mA[2] = -m * deti * ts;
			mA[3] = l1 * deti * ts;
			CircuitElement.StampConductance(node_index[PRI_T], node_index[PRI_B], mA[0]);
			CircuitElement.StampVCCurrentSource(node_index[PRI_T], node_index[PRI_B], node_index[SEC_T], node_index[SEC_B], mA[1]);
			CircuitElement.StampVCCurrentSource(node_index[SEC_T], node_index[SEC_B], node_index[PRI_T], node_index[PRI_B], mA[2]);
			CircuitElement.StampConductance(node_index[SEC_T], node_index[SEC_B], mA[3]);
			CircuitElement.StampRightSide(node_index[PRI_T]);
			CircuitElement.StampRightSide(node_index[SEC_T]);
			CircuitElement.StampRightSide(node_index[PRI_B]);
			CircuitElement.StampRightSide(node_index[SEC_B]);
		}
		#endregion

		#region [method(Circuit)]
		public override void prepare_iteration() {
			var voltDiffP = volts[PRI_T] - volts[PRI_B];
			var voltDiffS = volts[SEC_T] - volts[SEC_B];
			mCurSourceValue[0] = voltDiffP * mA[0] + voltDiffS * mA[1] + Currents[0];
			mCurSourceValue[1] = voltDiffP * mA[2] + voltDiffS * mA[3] + Currents[1];
		}

		public override void do_iteration() {
			var r = CircuitElement.row_info[node_index[PRI_T] - 1].row;
			CircuitElement.right_side[r] -= mCurSourceValue[0];
			r = CircuitElement.row_info[node_index[PRI_B] - 1].row;
			CircuitElement.right_side[r] += mCurSourceValue[0];
			r = CircuitElement.row_info[node_index[SEC_T] - 1].row;
			CircuitElement.right_side[r] -= mCurSourceValue[1];
			r = CircuitElement.row_info[node_index[SEC_B] - 1].row;
			CircuitElement.right_side[r] += mCurSourceValue[1];
		}

		public override double get_current_into_node(int n) {
			if (n < 2) {
				return -Currents[n];
			}
			return Currents[n - 2];
		}

		public override void set_voltage(int n, double c) {
			volts[n] = c;
			var voltDiffP = volts[PRI_T] - volts[PRI_B];
			var voltDiffS = volts[SEC_T] - volts[SEC_B];
			Currents[0] = voltDiffP * mA[0] + voltDiffS * mA[1] + mCurSourceValue[0];
			Currents[1] = voltDiffP * mA[2] + voltDiffS * mA[3] + mCurSourceValue[1];
		}
		#endregion
	}
}
