namespace Circuit.Elements.Output {
    class StopTriggerElmE : BaseElement {
		public double TriggerVoltage;
		public int Type;
		public double Delay;

		public bool Triggered { get; private set; }
		public bool Stopped { get; private set; }
		public double TriggerTime { get; private set; }

		public StopTriggerElmE() : base() {
			TriggerVoltage = 1;
		}

		public StopTriggerElmE(StringTokenizer st) : base() {
			TriggerVoltage = st.nextTokenDouble();
			Type = st.nextTokenInt();
			Delay = st.nextTokenDouble();
		}

		public override int CirPostCount { get { return 1; } }

		public override double CirVoltageDiff { get { return CirVolts[0]; } }

		public override void CirReset() {
			Triggered = false;
		}

		public override void CirStepFinished() {
			Stopped = false;
			if (!Triggered && ((Type == 0 && CirVolts[0] >= TriggerVoltage) || (Type == 1 && CirVolts[0] <= TriggerVoltage))) {
				Triggered = true;
				TriggerTime = CirSim.Sim.Time;
			}
			if (Triggered && CirSim.Sim.Time >= TriggerTime + Delay) {
				Triggered = false;
				Stopped = true;
				CirSim.Sim.SetSimRunning(false);
			}
		}
	}
}
