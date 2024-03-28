namespace Circuit.Elements.Passive {
	class ElmCapacitor : BaseElement {
		double mCompResistance;
		double mCurSourceValue;

		public double Capacitance = 1e-5;
		public double VoltDiff = 1e-3;

		public override int TermCount { get { return 2; } }

		#region [method(Analyze)]
		public override void reset() {
			base.reset();
			current = mCurSourceValue = 0;
			VoltDiff = 1e-3;
		}

		public override void shorted() {
			base.reset();
			VoltDiff = current = mCurSourceValue = 0;
		}

		public override void stamp() {
			var n0 = node_index[0] - 1;
			var n1 = node_index[1] - 1;
			if (n0 < 0 || n1 < 0) {
				return;
			}
			mCompResistance = 0.5 * CircuitElement.delta_time / Capacitance;
			CircuitElement.StampResistor(node_index[0], node_index[1], mCompResistance);
			CircuitElement.StampRightSide(node_index[0]);
			CircuitElement.StampRightSide(node_index[1]);
		}
		#endregion

		#region [method(Circuit)]
		public override void prepare_iteration() {
			mCurSourceValue = -VoltDiff / mCompResistance - current;
		}

		public override void do_iteration() {
			var r = CircuitElement.row_info[node_index[0] - 1].row;
			CircuitElement.right_side[r] -= mCurSourceValue;
			r = CircuitElement.row_info[node_index[1] - 1].row;
			CircuitElement.right_side[r] += mCurSourceValue;
		}

		public override void set_voltage(int n, double c) {
			volts[n] = c;
			VoltDiff = volts[0] - volts[1];
			current = VoltDiff / mCompResistance + mCurSourceValue;
		}
		#endregion
	}
}
