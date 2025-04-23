namespace Circuit.Elements.Active {
	class ElmAnalogSwitch : BaseElement {
		public const int R_ON = 0;
		public const int R_OFF = 1;
		public const int STATE = 0;
		public const int INVERT = 1;

		public override int TermCount { get { return 3; } }

		protected override void DoIteration() {
			var state = V[2] < 2.5;
			if (State[INVERT] != 0) {
				state = !state;
			}
			State[STATE] = state ? 1 : 0;
			Para[0] = state ? Para[R_OFF] : Para[R_ON];
			UpdateConductance(Nodes[0], Nodes[1], 1.0 / Para[0]);
		}

		protected override double GetCurrent(int n) {
			if (n == 0) {
				return -I[0];
			}
			if (n == 2) {
				return 0;
			}
			return I[0];
		}

		public override void SetVoltage(int n, double v) {
			V[n] = v;
			I[0] = (V[0] - V[1]) / Para[0];
		}
	}
}
