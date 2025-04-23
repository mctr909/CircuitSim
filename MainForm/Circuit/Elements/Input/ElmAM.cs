namespace Circuit.Elements.Input {
	class ElmAM : BaseElement {
		public double CarrierFreq = 1000;
		public double SignalFreq = 50;
		public double Depth = 0.1;
		public double MaxVoltage = 5;
		public double Phase = 0.0;

		public double mFreqTimeZero = 0.0;

		public override int TermCount { get { return 1; } }

		public override double VoltageDiff { get { return V[0]; } }

		protected override void DoIteration() {
			var th = 2 * Math.PI * (CircuitState.Time - mFreqTimeZero);
			var v = (Math.Sin(th * SignalFreq + Phase) * Depth + 2 - Depth) / 2 * Math.Sin(th * CarrierFreq) * MaxVoltage;
			UpdateVoltageSource(VoltSource, v);
		}
	}
}
