namespace Circuit.Elements.Logic {
	class ElmInvertingSchmitt : BaseElement {
		public double SlewRate = 0.5; // V/ns
		public double LowerTrigger = 1.66;
		public double UpperTrigger = 3.33;
		public double LogicOnLevel = 5;
		public double LogicOffLevel = 0;

		protected bool mState;

		public override int TermCount { get { return 2; } }

		public override double VoltageDiff { get { return Volts[0]; } }

		public override int VoltageSourceCount { get { return 1; } }

		// there is no current path through the InvertingSchmitt input, but there
		// is an indirect path through the output to ground.
		public override bool GetConnection(int n1, int n2) { return false; }

		public override bool HasGroundConnection(int n1) { return n1 == 1; }

		public override void Stamp() {
			CircuitElement.StampVoltageSource(0, Nodes[1], mVoltSource);
		}

		public override void DoIteration() {
			double v0 = Volts[1];
			double _out;
			if (mState) {//Output is high
				if (Volts[0] > UpperTrigger)//Input voltage high enough to set output low
				{
					mState = false;
					_out = LogicOffLevel;
				} else {
					_out = LogicOnLevel;
				}
			} else {//Output is low
				if (Volts[0] < LowerTrigger)//Input voltage low enough to set output high
				{
					mState = true;
					_out = LogicOnLevel;
				} else {
					_out = LogicOffLevel;
				}
			}
			double maxStep = SlewRate * CircuitElement.TimeStep * 1e9;
			_out = Math.Max(Math.Min(v0 + maxStep, _out), v0 - maxStep);
			CircuitElement.UpdateVoltageSource(mVoltSource, _out);
		}

		public override double GetCurrentIntoNode(int n) {
			if (n == 1) {
				return Current;
			}
			return 0;
		}
	}
}
