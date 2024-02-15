namespace Circuit.Elements.Input {
	class ElmAM : BaseElement {
		public double CarrierFreq;
		public double SignalFreq;
		public double Depth;
		public double MaxVoltage;
		public double Phase;

		double mFreqTimeZero;

		public ElmAM() : base() {
			MaxVoltage = 5;
			CarrierFreq = 1000;
			SignalFreq = 50;
			Depth = 0.1;
			Phase = 0.0;
			Reset();
		}

		public override int TermCount { get { return 1; } }

		public override int VoltageSourceCount { get { return 1; } }

		public override double VoltageDiff { get { return Volts[0]; } }

		public override void Reset() {
			mFreqTimeZero = 0;
		}

		public override bool HasGroundConnection(int n1) { return true; }

		public override void Stamp() {
			CircuitElement.StampVoltageSource(0, Nodes[0], mVoltSource);
		}

		public override void DoIteration() {
			var vn = CircuitElement.Nodes.Count + mVoltSource;
			var row = CircuitElement.RowInfo[vn - 1].MapRow;
			var th = 2 * Math.PI * (CircuitElement.Time - mFreqTimeZero);
			CircuitElement.RightSide[row] += (Math.Sin(th * SignalFreq + Phase) * Depth + 2 - Depth) / 2 * Math.Sin(th * CarrierFreq) * MaxVoltage;
		}
	}
}
