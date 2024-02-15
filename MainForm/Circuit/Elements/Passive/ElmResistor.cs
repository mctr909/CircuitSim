namespace Circuit.Elements.Passive {
	class ElmResistor : BaseElement {
		public double Resistance = 1000;

		public override int TermCount { get { return 2; } }

		public override void Stamp() {
			var n0 = Nodes[0] - 1;
			var n1 = Nodes[1] - 1;
			if (n0 < 0 || n1 < 0) {
				return;
			}
			var g = 1.0 / Resistance;
			CircuitElement.Matrix[n0, n0] += g;
			CircuitElement.Matrix[n1, n1] += g;
			CircuitElement.Matrix[n0, n1] -= g;
			CircuitElement.Matrix[n1, n0] -= g;
		}

		public override void SetVoltage(int n, double c) {
			Volts[n] = c;
			Current = (Volts[0] - Volts[1]) / Resistance;
		}
	}
}
