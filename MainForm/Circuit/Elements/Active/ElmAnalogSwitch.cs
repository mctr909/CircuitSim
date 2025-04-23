namespace Circuit.Elements.Active {
	class ElmAnalogSwitch : BaseElement {
		public double R_ON = 100;
		public double R_OFF = 1e8;
		public bool INVERT;
		public bool STATE;

		public override int TermCount { get { return 3; } }

		protected override void DoIteration() {
			STATE = V[2] < 2.5;
			if (INVERT) {
				STATE = !STATE;
			}
			Params[0] = STATE ? R_OFF : R_ON;
			UpdateConductance(Nodes[0], Nodes[1], 1.0 / Params[0]);
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
			I[0] = (V[0] - V[1]) / Params[0];
		}
	}
}
