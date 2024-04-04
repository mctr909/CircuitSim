namespace Circuit.Elements.Input {
	class ElmFM : BaseElement {
		public double CarrierFreq = 1000;
		public double Signalfreq = 40;
		public double MaxVoltage = 5;
		public double Deviation = 200;

		double mFreqTimeZero = 0;
		double mLastTime = 0;
		double mCounter = 0;

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
			var deltaT = CircuitState.Time - mLastTime;
			var signalAmplitude = Math.Sin(2 * Math.PI * (CircuitState.Time - mFreqTimeZero) * Signalfreq);
			mCounter += (CarrierFreq + (signalAmplitude * Deviation)) * deltaT;
			var v = Math.Sin(2 * Math.PI * mCounter) * MaxVoltage;
			UpdateVoltage(mVoltSource, v);
			mLastTime = CircuitState.Time;
		}
	}
}
