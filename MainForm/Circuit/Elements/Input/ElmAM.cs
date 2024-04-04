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

		public override double GetVoltageDiff() {
			return NodeVolts[0];
		}

		public override bool HasGroundConnection(int nodeIndex) { return true; }

		public override void Reset() {
			mFreqTimeZero = 0;
		}

		public override void Stamp() {
			StampVoltageSource(0, NodeId[0], mVoltSource);
		}

		public override void DoIteration() {
			var th = 2 * Math.PI * (CircuitState.Time - mFreqTimeZero);
			var v = (Math.Sin(th * SignalFreq + Phase) * Depth + 2 - Depth) / 2 * Math.Sin(th * CarrierFreq) * MaxVoltage;
			UpdateVoltage(mVoltSource, v);
		}
	}
}
