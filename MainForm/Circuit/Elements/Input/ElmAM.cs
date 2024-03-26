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

		public override double VoltageDiff { get { return Volts[0]; } }

		public override bool HasGroundConnection(int n1) { return true; }

		public override void Reset() {
			mFreqTimeZero = 0;
		}

		public override void Stamp() {
			CircuitElement.StampVoltageSource(0, Nodes[0], mVoltSource);
		}

		public override void DoIteration() {
			var vn = CircuitElement.nodes.Length + mVoltSource;
			var row = CircuitElement.row_info[vn - 1].row;
			var th = 2 * Math.PI * (CircuitElement.time - mFreqTimeZero);
			CircuitElement.right_side[row] += (Math.Sin(th * SignalFreq + Phase) * Depth + 2 - Depth) / 2 * Math.Sin(th * CarrierFreq) * MaxVoltage;
		}
	}
}
