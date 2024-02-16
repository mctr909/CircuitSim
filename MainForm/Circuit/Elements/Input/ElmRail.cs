﻿namespace Circuit.Elements.Input {
	class ElmRail : ElmVoltage {
		public override int TermCount { get { return 1; } }

		public override double VoltageDiff { get { return Volts[0]; } }

		public override bool HasGroundConnection(int n1) { return true; }

		public override void Stamp() {
			int n0 = Nodes[0] - 1;
			int vn = CircuitElement.Nodes.Count + mVoltSource - 1;
			if (n0 < 0 || vn < 0) {
				return;
			}
			CircuitElement.Matrix[vn, n0] += 1;
			CircuitElement.Matrix[n0, vn] -= 1;
			if (WaveForm == WAVEFORM.DC) {
				CircuitElement.RightSide[vn] += GetVoltage();
			} else {
				CircuitElement.RowInfo[vn].RightChanges = true;
			}
		}
	}
}