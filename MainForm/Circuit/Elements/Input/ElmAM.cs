namespace Circuit.Elements.Input {
	class ElmAM : BaseElement {
		public double CarrierFreq = 1000;
		public double SignalFreq = 50;
		public double Depth = 0.1;
		public double MaxVoltage = 5;
		public double Phase = 0.0;

		double mFreqTimeZero = 0.0;

		public override int TermCount { get { return 1; } }

		public override int VoltageSourceCount { get { return 1; } }

		public override double voltage_diff() {
			return volts[0];
		}

		public override bool has_ground_connection(int n1) { return true; }

		public override void reset() {
			mFreqTimeZero = 0;
		}

		public override void stamp() {
			CircuitElement.StampVoltageSource(0, node_index[0], m_volt_source);
		}

		public override void do_iteration() {
			var vn = CircuitElement.nodes.Length + m_volt_source;
			var row = CircuitElement.row_info[vn - 1].row;
			var th = 2 * Math.PI * (CircuitElement.time - mFreqTimeZero);
			CircuitElement.right_side[row] += (Math.Sin(th * SignalFreq + Phase) * Depth + 2 - Depth) / 2 * Math.Sin(th * CarrierFreq) * MaxVoltage;
		}
	}
}
