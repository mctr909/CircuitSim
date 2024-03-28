namespace Circuit.Elements.Passive {
	class ElmPolarCapacitor : ElmCapacitor {
		public double MaxNegativeVoltage = 1.0;

		public override void finish_iteration() {
			var v = voltage_diff();
			if (v < 0 && v < -MaxNegativeVoltage) {
				CircuitElement.stopped = true;
			}
		}
	}
}
