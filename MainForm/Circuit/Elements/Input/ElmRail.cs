namespace Circuit.Elements.Input {
	class ElmRail : ElmVoltage {
		public override int TermCount { get { return 1; } }

		public override double voltage_diff() {
			return volts[0];
		}

		public override bool has_ground_connection(int n1) { return true; }

		public override void stamp() {
			int n0 = node_index[0] - 1;
			int vn = CircuitElement.nodes.Length + m_volt_source - 1;
			if (n0 < 0 || vn < 0) {
				return;
			}
			CircuitElement.matrix[vn, n0] += 1;
			CircuitElement.matrix[n0, vn] -= 1;
			if (WaveForm == WAVEFORM.DC) {
				CircuitElement.right_side[vn] += GetVoltage();
			} else {
				CircuitElement.row_info[vn].right_changes = true;
			}
		}
	}
}
