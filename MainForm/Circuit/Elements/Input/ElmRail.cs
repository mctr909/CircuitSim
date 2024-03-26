namespace Circuit.Elements.Input {
	class ElmRail : ElmVoltage {
		public override int TermCount { get { return 1; } }

		public override double VoltageDiff { get { return Volts[0]; } }

		public override bool HasGroundConnection(int n1) { return true; }

		public override void Stamp() {
			int n0 = Nodes[0] - 1;
			int vn = CircuitElement.nodes.Length + mVoltSource - 1;
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
