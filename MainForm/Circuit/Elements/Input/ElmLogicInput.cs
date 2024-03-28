using Circuit.Elements.Passive;

namespace Circuit.Elements.Input {
	class ElmLogicInput : ElmSwitch {
		public double VHigh = 5;
		public double VLow = 0;

		public override int VoltageSourceCount { get { return 1; } }

		public override int TermCount { get { return 1; } }

		public override double voltage_diff() {
			return volts[0];
		}

		public override bool has_ground_connection(int n1) { return true; }

		public override void stamp() {
			double v = 0 != Position ? VHigh : VLow;
			CircuitElement.StampVoltageSource(0, node_index[0], m_volt_source, v);
		}

		public override double get_current_into_node(int n) {
			return -current;
		}

		public override void set_current(int vs, double c) { current = -c; }
	}
}
