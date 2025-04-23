namespace Circuit.Elements.Logic {
	class ElmSchmitt : ElmInvertingSchmitt {
		public ElmSchmitt() : base() { }

		protected override void DoIteration() {
			double v0 = V[1];
			double _out;
			if (mState) {//Output is high
				if (V[0] > UpperTrigger)//Input voltage high enough to set output high
				{
					mState = false;
					_out = LogicOnLevel;
				} else {
					_out = LogicOffLevel;
				}
			} else {//Output is low
				if (V[0] < LowerTrigger)//Input voltage low enough to set output low
				{
					mState = true;
					_out = LogicOffLevel;
				} else {
					_out = LogicOnLevel;
				}
			}
			double maxStep = SlewRate * CircuitState.DeltaTime * 1e9;
			_out = Math.Max(Math.Min(v0 + maxStep, _out), v0 - maxStep);
			UpdateVoltageSource(VoltSource, _out);
		}

		protected override double GetCurrent(int n) { return (n == 1) ? I[0] : 0; }
	}
}
