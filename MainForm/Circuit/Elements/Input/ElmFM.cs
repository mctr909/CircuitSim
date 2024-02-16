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

		public override double VoltageDiff { get { return Volts[0]; } }

		public override void Reset() {
			mFreqTimeZero = 0;
		}

		public override bool HasGroundConnection(int n1) { return true; }

		public override void Stamp() {
			CircuitElement.StampVoltageSource(0, Nodes[0], mVoltSource);
		}

		public override void DoIteration() {
			var deltaT = CircuitElement.Time - mLastTime;
			var signalAmplitude = Math.Sin(2 * Math.PI * (CircuitElement.Time - mFreqTimeZero) * Signalfreq);
			mCounter += (CarrierFreq + (signalAmplitude * Deviation)) * deltaT;
			var vn = CircuitElement.Nodes.Count + mVoltSource;
			var row = CircuitElement.RowInfo[vn - 1].MapRow;
			CircuitElement.RightSide[row] += Math.Sin(2 * Math.PI * mCounter) * MaxVoltage;
			mLastTime = CircuitElement.Time;
		}
	}
}
