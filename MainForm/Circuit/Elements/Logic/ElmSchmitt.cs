namespace Circuit.Elements.Logic {
	class ElmSchmitt : ElmInvertingSchmitt {
		public ElmSchmitt() : base() { }

		public override void do_iteration() {
			double v0 = volts[1];
			double _out;
			if (mState) {//Output is high
				if (volts[0] > UpperTrigger)//Input voltage high enough to set output high
				{
					mState = false;
					_out = LogicOnLevel;
				} else {
					_out = LogicOffLevel;
				}
			} else {//Output is low
				if (volts[0] < LowerTrigger)//Input voltage low enough to set output low
				{
					mState = true;
					_out = LogicOffLevel;
				} else {
					_out = LogicOnLevel;
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
	}
}
