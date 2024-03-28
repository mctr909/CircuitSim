namespace Circuit.Elements.Passive {
	class ElmSwitch : BaseElement {
		public bool Momentary = false;
		public int Position = 0;
		public int PosCount = 2;
		public int Link = 0;

		public override int TermCount { get { return 2; } }
		public override bool IsWire { get { return Position == 0; } }
		public override int VoltageSourceCount { get { return (1 == Position) ? 0 : 1; } }

		public override bool has_connection(int n1, int n2) { return 0 == Position; }

		public override void stamp() {
			if (Position == 0) {
				CircuitElement.StampVoltageSource(node_index[0], node_index[1], m_volt_source, 0);
			}
		}

		public override void set_voltage(int n, double c) {
			volts[n] = c;
			if (Position == 1) {
				current = 0;
			}
		}
	}
}
