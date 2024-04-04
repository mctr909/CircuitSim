namespace Circuit.Elements.Input {
	class ElmRail : ElmVoltage {
		public override int TermCount { get { return 1; } }

		public override double GetVoltageDiff() {
			return NodeVolts[0];
		}

		public override bool HasGroundConnection(int nodeIndex) { return true; }

		public override void Stamp() {
			if (WaveForm == WAVEFORM.DC) {
				StampVoltageSource(NodeId[0], mVoltSource, GetVoltage());
			} else {
				StampVoltageSource(NodeId[0], mVoltSource);
			}
		}
	}
}
