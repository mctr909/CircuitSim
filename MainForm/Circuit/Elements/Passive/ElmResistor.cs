namespace Circuit.Elements.Passive {
	class ElmResistor : BaseElement {
		public double Resistance = 1000;

		public override int TermCount { get { return 2; } }

		public override void stamp() {
			CircuitElement.StampResistor(node_index[0], node_index[1], Resistance);
		}

		public override void set_voltage(int n, double c) {
			volts[n] = c;
			current = (volts[0] - volts[1]) / Resistance;
		}
	}
}
