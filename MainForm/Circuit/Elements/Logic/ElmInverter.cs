namespace Circuit.Elements.Logic {
	class ElmInverter : BaseElement {
		public double SlewRate = 0.5; /* V/ns */
		public double HighVoltage = ElmGate.LastHighVoltage;

		double mLastOutputVoltage;

		public override int TermCount { get { return 2; } }

		public override int VoltageSourceCount { get { return 1; } }

		public override double voltage_diff() {
			return volts[0];
		}

		#region [method(Analyze)]
		/* there is no current path through the inverter input,
         * but there is an indirect path through the output to ground. */
		public override bool has_connection(int n1, int n2) { return false; }

		public override bool has_ground_connection(int n1) { return n1 == 1; }

		public override void stamp() {
			CircuitElement.StampVoltageSource(0, node_index[1], m_volt_source);
		}
		#endregion

		#region [method(Circuit)]
		public override void prepare_iteration() {
			mLastOutputVoltage = volts[1];
		}

		public override void do_iteration() {
			double v = volts[0] > HighVoltage * .5 ? 0 : HighVoltage;
			double maxStep = SlewRate * CircuitElement.delta_time * 1e9;
			v = Math.Max(Math.Min(mLastOutputVoltage + maxStep, v), mLastOutputVoltage - maxStep);
			CircuitElement.UpdateVoltageSource(m_volt_source, v);
		}

		public override double get_current_into_node(int n) {
			if (n == 1) {
				return current;
			}
			return 0;
		}
		#endregion
	}
}
