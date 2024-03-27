namespace Circuit.Elements.Passive {
	class ElmPolarCapacitor : ElmCapacitor {
		public double MaxNegativeVoltage = 1.0;

		public override void FinishIteration() {
			var v = VoltageDiff();
			if (v < 0 && v < -MaxNegativeVoltage) {
				CircuitElement.stopped = true;
			}
		}
	}
}
