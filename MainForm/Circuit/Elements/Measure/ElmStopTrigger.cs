namespace Circuit.Elements.Measure {
	class ElmStopTrigger : BaseElement {
		public double TriggerVoltage = 1;
		public int Type;
		public double Delay;

		public bool Triggered;
		public bool Stopped;
		public double TriggerTime;

		public override int TermCount { get { return 1; } }

		public override double voltage_diff() {
			return volts[0];
		}

		public override void reset() {
			Triggered = false;
		}

		public override void finish_iteration() {
			Stopped = false;
			if (!Triggered && ((Type == 0 && volts[0] >= TriggerVoltage) || (Type == 1 && volts[0] <= TriggerVoltage))) {
				Triggered = true;
				TriggerTime = CircuitElement.time;
			}
			if (Triggered && CircuitElement.time >= TriggerTime + Delay) {
				Triggered = false;
				Stopped = true;
				CircuitElement.stopped = true;
			}
		}
	}
}
