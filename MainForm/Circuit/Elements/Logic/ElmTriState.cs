namespace Circuit.Elements.Logic {
	class ElmTriState : BaseElement {
		public double Ron = 0.1;
		public double Roff = 1e10;
		public bool Open;

		double mResistance;

		public override int TermCount { get { return 3; } }

		public override int InternalNodeCount { get { return 1; } }

		public override int VoltageSourceCount { get { return 1; } }

		#region [method(Analyze)]
		/* there is no current path through the input, but there
         * is an indirect path through the output to ground. */
		public override bool has_connection(int n1, int n2) { return false; }

		public override bool has_ground_connection(int n1) {
			return n1 == 1;
		}

		public override void stamp() {
			CircuitElement.StampVoltageSource(0, node_index[3], m_volt_source);
			CircuitElement.StampNonLinear(node_index[3]);
			CircuitElement.StampNonLinear(node_index[1]);
		}
		#endregion

		#region [method(Circuit)]
		public override void do_iteration() {
			Open = volts[2] < 2.5;
			mResistance = Open ? Roff : Ron;
			CircuitElement.StampResistor(node_index[3], node_index[1], mResistance);
			CircuitElement.UpdateVoltageSource(m_volt_source, volts[0] > 2.5 ? 5 : 0);
		}

		public override double get_current_into_node(int n) {
			if (n == 1) {
				return current;
			}
			return 0;
		}

		public override void set_voltage(int n, double c) {
			volts[n] = c;
			current = (volts[0] - volts[1]) / mResistance;
		}
		#endregion
	}
}
