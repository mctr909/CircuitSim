namespace Circuit.Elements.Logic {
	class ElmInvertingSchmitt : BaseElement {
		public double SlewRate = 0.5; // V/ns
		public double LowerTrigger = 1.66;
		public double UpperTrigger = 3.33;
		public double LogicOnLevel = 5;
		public double LogicOffLevel = 0;

		protected bool mState;

		public override int TermCount { get { return 2; } }

		public override int VoltageSourceCount { get { return 1; } }

		public override double voltage_diff() {
			return volts[0];
		}

		#region [method(Analyze)]
		// there is no current path through the InvertingSchmitt input, but there
		// is an indirect path through the output to ground.
		public override bool has_connection(int n1, int n2) { return false; }

		public override bool has_ground_connection(int n1) { return n1 == 1; }

		public override void stamp() {
			CircuitElement.StampVoltageSource(0, node_index[1], m_volt_source);
		}
		#endregion

		#region [method(Circuit)]
		public override void do_iteration() {
			double v0 = volts[1];
			double _out;
			if (mState) {//Output is high
				if (volts[0] > UpperTrigger)//Input voltage high enough to set output low
				{
					mState = false;
					_out = LogicOffLevel;
				} else {
					_out = LogicOnLevel;
				}
			} else {//Output is low
				if (volts[0] < LowerTrigger)//Input voltage low enough to set output high
				{
					mState = true;
					_out = LogicOnLevel;
				} else {
					_out = LogicOffLevel;
				}
			}
			double maxStep = SlewRate * CircuitElement.delta_time * 1e9;
			_out = Math.Max(Math.Min(v0 + maxStep, _out), v0 - maxStep);
			CircuitElement.UpdateVoltageSource(m_volt_source, _out);
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
