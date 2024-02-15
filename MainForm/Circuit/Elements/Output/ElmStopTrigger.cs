﻿namespace Circuit.Elements.Output {
	class ElmStopTrigger : BaseElement {
		public double TriggerVoltage;
		public int Type;
		public double Delay;

		public bool Triggered { get; private set; }
		public bool Stopped { get; private set; }
		public double TriggerTime { get; private set; }

		public ElmStopTrigger() : base() {
			TriggerVoltage = 1;
		}

		public override int TermCount { get { return 1; } }

		public override double VoltageDiff { get { return Volts[0]; } }

		public override void Reset() {
			Triggered = false;
		}

		public override void IterationFinished() {
			Stopped = false;
			if (!Triggered && ((Type == 0 && Volts[0] >= TriggerVoltage) || (Type == 1 && Volts[0] <= TriggerVoltage))) {
				Triggered = true;
				TriggerTime = CircuitElement.Time;
			}
			if (Triggered && CircuitElement.Time >= TriggerTime + Delay) {
				Triggered = false;
				Stopped = true;
				CircuitElement.Stopped = true;
			}
		}
	}
}
