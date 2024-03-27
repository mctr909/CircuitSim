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

		public override double VoltageDiff() {
			return Volts[0];
		}

		public override bool HasGroundConnection(int n1) { return true; }

		public override void Reset() {
			mFreqTimeZero = 0;
		}

		public override void Stamp() {
			CircuitElement.StampVoltageSource(0, NodeIndex[0], mVoltSource);
		}

		public override void DoIteration() {
			var deltaT = CircuitElement.time - mLastTime;
			var signalAmplitude = Math.Sin(2 * Math.PI * (CircuitElement.time - mFreqTimeZero) * Signalfreq);
			mCounter += (CarrierFreq + (signalAmplitude * Deviation)) * deltaT;
			var vn = CircuitElement.nodes.Length + mVoltSource;
			var row = CircuitElement.row_info[vn - 1].row;
			CircuitElement.right_side[row] += Math.Sin(2 * Math.PI * mCounter) * MaxVoltage;
			mLastTime = CircuitElement.time;
		}
	}
}
