namespace Circuit.Elements.Passive {
	class ElmResistor : BaseElement {
		public double Resistance = 1000;

		public override int TermCount { get { return 2; } }

		public override void Stamp() {
			CircuitElement.StampResistor(Nodes[0], Nodes[1], Resistance);
		}

		public override void SetVoltage(int n, double c) {
			Volts[n] = c;
			Current = (Volts[0] - Volts[1]) / Resistance;
		}
	}
}
