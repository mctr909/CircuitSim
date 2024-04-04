namespace Circuit.Elements.Passive {
	class ElmPolarCapacitor : ElmCapacitor {
		public double MaxNegativeVoltage = 1.0;

		public override void FinishIteration() {
			var v = GetVoltageDiff();
			if (v < 0 && v < -MaxNegativeVoltage) {
				CircuitState.Stopped = true;
			}
		}
	}
}
