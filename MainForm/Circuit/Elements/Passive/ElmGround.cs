namespace Circuit.Elements.Passive {
	class ElmGround : BaseElement {
		public override int VoltageSourceCount { get { return 1; } }

		public override int TermCount { get { return 1; } }

		public override double voltage_diff() {
			return 0;
		}

		#region [method(Analyze)]
		public override bool has_ground_connection(int n1) { return true; }

		public override void stamp() {
			CircuitElement.StampVoltageSource(0, node_index[0], m_volt_source, 0);
		}
		#endregion

		#region [method(Circuit)]
		public override double get_current_into_node(int n) { return -current; }

		public override void set_current(int x, double c) { current = -c; }
		#endregion
	}
}
