namespace Circuit.Elements.Passive {
	class ElmSwitchMulti : ElmSwitch {
		public int ThrowCount = 2;

		public override bool IsWire { get { return true; } }

		public override int VoltageSourceCount { get { return 1; } }

		public override int TermCount { get { return 1 + ThrowCount; } }

		public override bool has_connection(int n1, int n2) {
			return ComparePair(n1, n2, 0, 1 + Position);
		}

		public override void stamp() {
			CircuitElement.StampVoltageSource(node_index[0], node_index[Position + 1], m_volt_source, 0);
		}

		public override double get_current_into_node(int n) {
			if (n == 0) {
				return -current;
			}
			if (n == Position + 1) {
				return current;
			}
			return 0;
		}

	}
}
