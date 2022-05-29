namespace Circuit.Elements.Output {
    class StopTriggerElm : BaseElement {
		public double TriggerVoltage;
		public int Type;
		public double Delay;

		public bool Triggered { get; private set; }
		public bool Stopped { get; private set; }
		public double TriggerTime { get; private set; }

		public StopTriggerElm() : base() {
			TriggerVoltage = 1;
		}

		public StopTriggerElm(StringTokenizer st) : base() {
			TriggerVoltage = st.nextTokenDouble();
			Type = st.nextTokenInt();
			Delay = st.nextTokenDouble();
		}

		public override int PostCount { get { return 1; } }

		public override double VoltageDiff { get { return Volts[0]; } }

		public override void Reset() {
			Triggered = false;
		}

		public override void CirStepFinished() {
			Stopped = false;
			if (!Triggered && ((Type == 0 && Volts[0] >= TriggerVoltage) || (Type == 1 && Volts[0] <= TriggerVoltage))) {
				Triggered = true;
				TriggerTime = CirSimForm.Sim.Time;
			}
			if (Triggered && CirSimForm.Sim.Time >= TriggerTime + Delay) {
				Triggered = false;
				Stopped = true;
				CirSimForm.Sim.SetSimRunning(false);
			}
		}
	}
}
