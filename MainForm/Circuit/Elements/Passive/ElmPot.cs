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
			StampResistor(NodeId[0], NodeId[2], Resistance1);
			StampResistor(NodeId[2], NodeId[1], Resistance2);
		}

		public override double GetCurrent(int n) {
			if (n == 0) {
				return -Current1;
			}
			if (n == 1) {
				return -Current2;
			}
			return -Current3;
		}

		public override void SetVoltage(int nodeIndex, double v) {
			NodeVolts[nodeIndex] = v;
			if (0.0 < Resistance1) { // avoid NaN
				Current1 = (NodeVolts[V_L] - NodeVolts[V_S]) / Resistance1;
				Current2 = (NodeVolts[V_R] - NodeVolts[V_S]) / Resistance2;
				Current3 = -Current1 - Current2;
			}
		}
	}
}
