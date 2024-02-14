namespace Circuit.Elements.Passive {
	class ElmPolarCapacitor : ElmCapacitor {
		public double MaxNegativeVoltage = 1.0;

		public override void IterationFinished() {
			if (VoltageDiff < 0 && VoltageDiff < -MaxNegativeVoltage) {
				Circuit.Stop(this);
			}
		}
	}
}
