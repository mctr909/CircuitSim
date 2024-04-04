namespace Circuit.Elements.Logic {
	class ElmSchmitt : ElmInvertingSchmitt {
		public ElmSchmitt() : base() { }

		public override void DoIteration() {
			double v0 = NodeVolts[1];
			double _out;
			if (mState) {//Output is high
				if (NodeVolts[0] > UpperTrigger)//Input voltage high enough to set output high
				{
					mState = false;
					_out = LogicOnLevel;
				} else {
					_out = LogicOffLevel;
				}
			} else {//Output is low
				if (NodeVolts[0] < LowerTrigger)//Input voltage low enough to set output low
				{
					mState = true;
					_out = LogicOffLevel;
				} else {
					_out = LogicOnLevel;
				}
			}
			double maxStep = SlewRate * CircuitState.DeltaTime * 1e9;
			_out = Math.Max(Math.Min(v0 + maxStep, _out), v0 - maxStep);
			UpdateVoltage(mVoltSource, _out);
		}

		public override double GetCurrent(int n) {
			if (n == 1) {
				return Current;
			}
			return 0;
		}
	}
}
