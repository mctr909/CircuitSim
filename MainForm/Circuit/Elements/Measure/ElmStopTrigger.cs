namespace Circuit.Elements.Measure {
	class ElmStopTrigger : BaseElement {
		public double TriggerVoltage = 1;
		public int Type;
		public double Delay;

		public bool Triggered;
		public bool Stopped;
		public double TriggerTime;

		public override int TermCount { get { return 1; } }

		public override double GetVoltageDiff() {
			return NodeVolts[0];
		}

		public override void Reset() {
			Triggered = false;
		}

		public override void FinishIteration() {
			Stopped = false;
			if (!Triggered && ((Type == 0 && NodeVolts[0] >= TriggerVoltage) || (Type == 1 && NodeVolts[0] <= TriggerVoltage))) {
				Triggered = true;
				TriggerTime = CircuitState.Time;
			}
			if (Triggered && CircuitState.Time >= TriggerTime + Delay) {
				Triggered = false;
				Stopped = true;
				CircuitState.Stopped = true;
			}
		}
	}
}
