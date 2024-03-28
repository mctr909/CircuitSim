namespace Circuit.Elements.Passive {
	class ElmInductor : BaseElement {
		double mCompResistance;
		double mCurSourceValue;

		public double Inductance = 1e-4;

		public override int TermCount { get { return 2; } }

		#region [method(Analyze)]
		public override void reset() {
			current = volts[0] = volts[1] = mCurSourceValue = 0;
		}

		public override void stamp() {
			mCompResistance = 2 * Inductance / CircuitElement.delta_time;
			CircuitElement.StampResistor(node_index[0], node_index[1], mCompResistance);
			CircuitElement.StampRightSide(node_index[0]);
			CircuitElement.StampRightSide(node_index[1]);
		}
		#endregion

		#region [method(Circuit)]
		public override void prepare_iteration() {
			mCurSourceValue = (volts[0] - volts[1]) / mCompResistance + current;
		}

		public override void do_iteration() {
			var r = CircuitElement.row_info[node_index[0] - 1].row;
			CircuitElement.right_side[r] -= mCurSourceValue;
			r = CircuitElement.row_info[node_index[1] - 1].row;
			CircuitElement.right_side[r] += mCurSourceValue;
		}

		public override void set_voltage(int n, double c) {
			volts[n] = c;
			current = (volts[0] - volts[1]) / mCompResistance + mCurSourceValue;
		}
		#endregion
	}
}
