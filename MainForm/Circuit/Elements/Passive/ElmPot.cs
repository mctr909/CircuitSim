namespace Circuit.Elements.Passive {
	class ElmPot : BaseElement {
		public const int V_L = 0;
		public const int V_R = 1;
		public const int V_S = 2;

		public double Position = 0.5;
		public double MaxResistance = 1000;

		public double Resistance1;
		public double Resistance2;
		public double Current1;
		public double Current2;
		public double Current3;

		public override int TermCount { get { return 3; } }

		public override void Stamp() {
			Resistance1 = MaxResistance * Position;
			Resistance2 = MaxResistance * (1 - Position);
			CircuitElement.StampResistor(NodeIndex[0], NodeIndex[2], Resistance1);
			CircuitElement.StampResistor(NodeIndex[2], NodeIndex[1], Resistance2);
		}

		public override double GetCurrentIntoNode(int n) {
			if (n == 0) {
				return -Current1;
			}
			if (n == 1) {
				return -Current2;
			}
			return -Current3;
		}

		public override void SetVoltage(int n, double c) {
			Volts[n] = c;
			if (0.0 < Resistance1) { // avoid NaN
				Current1 = (Volts[V_L] - Volts[V_S]) / Resistance1;
				Current2 = (Volts[V_R] - Volts[V_S]) / Resistance2;
				Current3 = -Current1 - Current2;
			}
		}
	}
}
