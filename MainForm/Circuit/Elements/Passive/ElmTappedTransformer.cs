namespace Circuit.Elements.Passive {
	class ElmTappedTransformer : BaseElement {
		public double PInductance = 0.01;
		public double Ratio = 1.0;
		public double CouplingCoef = 0.999;
		public int Polarity = 1;

		public double[] Currents = new double[4];

		double[] mA = new double[9];
		double[] mVoltageDiff = new double[3];
		double[] mCurSourceValue = new double[3];

		public override int TermCount { get { return 5; } }

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
			volts[0] = volts[1] = volts[2] = volts[3] = volts[4] = 0;
			Currents[0] = Currents[1] = Currents[2] = Currents[3] = 0;
			mCurSourceValue[0] = mCurSourceValue[1] = mCurSourceValue[2] = 0;
		}

		public override void stamp() {
			// equations for transformer:
			// v1 = L1 di1/dt + M1 di2/dt + M1 di3/dt
			// v2 = M1 di1/dt + L2 di2/dt + M2 di3/dt
			// v3 = M1 di1/dt + M2 di2/dt + L2 di3/dt
			// we invert that to get:
			// di1/dt = a1 v1 + a2 v2 + a3 v3
			// di2/dt = a4 v1 + a5 v2 + a6 v3
			// di3/dt = a7 v1 + a8 v2 + a9 v3
			// integrate di1/dt using trapezoidal approx and we get:
			// i1(t2) = i1(t1) + dt/2 (i1(t1) + i1(t2))
			// = i1(t1) + a1 dt/2 v1(t1)+a2 dt/2 v2(t1)+a3 dt/2 v3(t1) +
			// a1 dt/2 v1(t2)+a2 dt/2 v2(t2)+a3 dt/2 v3(t2)
			// the norton equivalent of this for i1 is:
			// a. current source, I = i1(t1) + a1 dt/2 v1(t1) + a2 dt/2 v2(t1)
			// + a3 dt/2 v3(t1)
			// b. resistor, G = a1 dt/2
			// c. current source controlled by voltage v2, G = a2 dt/2
			// d. current source controlled by voltage v3, G = a3 dt/2
			// and similarly for i2, i3
			//
			// first winding goes from node 0 to 1, second is from 2 to 3 to 4
			var l1 = PInductance;
			// second winding is split in half, so each part has half the turns;
			// we square the 1/2 to divide by 4
			var l2 = PInductance * Ratio * Ratio / 4;
			var m1 = CouplingCoef * Math.Sqrt(l1 * l2);
			// mutual inductance between two halves of the second winding
			// is equal to self-inductance of either half (slightly less
			// because the coupling is not perfect)
			var m2 = CouplingCoef * l2;
			// load pre-inverted matrix
			mA[0] = l2 + m2;
			mA[1] = mA[2] = mA[3] = mA[6] = -m1;
			mA[4] = mA[8] = (l1 * l2 - m1 * m1) / (l2 - m2);
			mA[5] = mA[7] = (m1 * m1 - l1 * m2) / (l2 - m2);
			var det = l1 * (l2 + m2) - 2 * m1 * m1;
			for (int i = 0; i != 9; i++) {
				mA[i] *= CircuitElement.delta_time / 2 / det;
			}
			CircuitElement.StampConductance(node_index[0], node_index[1], mA[0]);
			CircuitElement.StampVCCurrentSource(node_index[0], node_index[1], node_index[2], node_index[3], mA[1]);
			CircuitElement.StampVCCurrentSource(node_index[0], node_index[1], node_index[3], node_index[4], mA[2]);

			CircuitElement.StampVCCurrentSource(node_index[2], node_index[3], node_index[0], node_index[1], mA[3]);
			CircuitElement.StampConductance(node_index[2], node_index[3], mA[4]);
			CircuitElement.StampVCCurrentSource(node_index[2], node_index[3], node_index[3], node_index[4], mA[5]);

			CircuitElement.StampVCCurrentSource(node_index[3], node_index[4], node_index[0], node_index[1], mA[6]);
			CircuitElement.StampVCCurrentSource(node_index[3], node_index[4], node_index[2], node_index[3], mA[7]);
			CircuitElement.StampConductance(node_index[3], node_index[4], mA[8]);

			for (int i = 0; i != 5; i++) {
				CircuitElement.StampRightSide(node_index[i]);
			}
		}
		#endregion

		#region [method(Circuit)]
		public override void prepare_iteration() {
			mVoltageDiff[0] = volts[0] - volts[1];
			mVoltageDiff[1] = volts[2] - volts[3];
			mVoltageDiff[2] = volts[3] - volts[4];
			mCurSourceValue[0] = Currents[0];
			mCurSourceValue[0] += mA[0] * mVoltageDiff[0];
			mCurSourceValue[0] += mA[1] * mVoltageDiff[1];
			mCurSourceValue[0] += mA[2] * mVoltageDiff[2];
			mCurSourceValue[1] = Currents[1];
			mCurSourceValue[1] += mA[3] * mVoltageDiff[0];
			mCurSourceValue[1] += mA[4] * mVoltageDiff[1];
			mCurSourceValue[1] += mA[5] * mVoltageDiff[2];
			mCurSourceValue[2] = Currents[2];
			mCurSourceValue[2] += mA[6] * mVoltageDiff[0];
			mCurSourceValue[2] += mA[7] * mVoltageDiff[1];
			mCurSourceValue[2] += mA[8] * mVoltageDiff[2];
		}

		public override void do_iteration() {
			CircuitElement.StampCurrentSource(node_index[0], node_index[1], mCurSourceValue[0]);
			CircuitElement.StampCurrentSource(node_index[2], node_index[3], mCurSourceValue[1]);
			CircuitElement.StampCurrentSource(node_index[3], node_index[4], mCurSourceValue[2]);
		}

		public override double get_current_into_node(int n) {
			if (n == 0)
				return -Currents[0];
			if (n == 1)
				return Currents[0];
			if (n == 2)
				return -Currents[1];
			if (n == 3)
				return Currents[3];
			return Currents[2];
		}

		public override void set_voltage(int n, double c) {
			volts[n] = c;
			mVoltageDiff[0] = volts[0] - volts[1];
			mVoltageDiff[1] = volts[2] - volts[3];
			mVoltageDiff[2] = volts[3] - volts[4];
			Currents[0] = mCurSourceValue[0];
			Currents[0] += mA[0] * mVoltageDiff[0];
			Currents[0] += mA[1] * mVoltageDiff[1];
			Currents[0] += mA[2] * mVoltageDiff[2];
			Currents[1] = mCurSourceValue[1];
			Currents[1] += mA[3] * mVoltageDiff[0];
			Currents[1] += mA[4] * mVoltageDiff[1];
			Currents[1] += mA[5] * mVoltageDiff[2];
			Currents[2] = mCurSourceValue[2];
			Currents[2] += mA[6] * mVoltageDiff[0];
			Currents[2] += mA[7] * mVoltageDiff[1];
			Currents[2] += mA[8] * mVoltageDiff[2];
			// calc current of tap wire
			Currents[3] = Currents[1] - Currents[2];
		}
		#endregion
	}
}
