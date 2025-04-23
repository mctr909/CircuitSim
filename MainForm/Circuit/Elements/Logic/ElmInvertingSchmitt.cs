namespace Circuit.Elements.Logic {
	class ElmInvertingSchmitt : BaseElement {
		public double SlewRate = 0.5; // V/ns
		public double LowerTrigger = 1.66;
		public double UpperTrigger = 3.33;
		public double LogicOnLevel = 5;
		public double LogicOffLevel = 0;

		protected bool mState;

		public override double VoltageDiff { get { return V[0]; } }

		protected override void DoIteration() {
			double v0 = V[1];
			double _out;
			if (mState) {//Output is high
				if (V[0] > UpperTrigger)//Input voltage high enough to set output low
				{
					mState = false;
					_out = LogicOffLevel;
				} else {
					_out = LogicOnLevel;
				}
			} else {//Output is low
				if (V[0] < LowerTrigger)//Input voltage low enough to set output high
				{
					mState = true;
					_out = LogicOnLevel;
				} else {
					_out = LogicOffLevel;
				}
			}
			double maxStep = SlewRate * CircuitState.DeltaTime * 1e9;
			_out = Math.Max(Math.Min(v0 + maxStep, _out), v0 - maxStep);
			UpdateVoltageSource(VoltSource, _out);
		}

		protected override double GetCurrent(int n) { return (n == 1) ? I[0] : 0; }
	}
}
