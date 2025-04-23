namespace Circuit.Elements.Input {
	class ElmFM : BaseElement {
		public double CarrierFreq = 1000;
		public double Signalfreq = 40;
		public double MaxVoltage = 5;
		public double Deviation = 200;

		public double mFreqTimeZero = 0;
		double mLastTime = 0;
		double mCounter = 0;

		public override int TermCount { get { return 1; } }

		public override double VoltageDiff { get { return V[0]; } }

		protected override void DoIteration() {
			var deltaT = CircuitState.Time - mLastTime;
			var signalAmplitude = Math.Sin(2 * Math.PI * (CircuitState.Time - mFreqTimeZero) * Signalfreq);
			mCounter += (CarrierFreq + (signalAmplitude * Deviation)) * deltaT;
			var v = Math.Sin(2 * Math.PI * mCounter) * MaxVoltage;
			UpdateVoltageSource(VoltSource, v);
			mLastTime = CircuitState.Time;
		}
	}
}
