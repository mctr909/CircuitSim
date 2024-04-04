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

		public override double GetVoltageDiff() {
			return NodeVolts[0];
		}

		#region [method(Analyze)]
		// there is no current path through the InvertingSchmitt input, but there
		// is an indirect path through the output to ground.
		public override bool HasConnection(int n1, int n2) { return false; }

		public override bool HasGroundConnection(int nodeIndex) { return nodeIndex == 1; }

		public override void Stamp() {
			StampVoltageSource(0, NodeId[1], mVoltSource);
		}
		#endregion

		#region [method(Circuit)]
		public override void DoIteration() {
			double v0 = NodeVolts[1];
			double _out;
			if (mState) {//Output is high
				if (NodeVolts[0] > UpperTrigger)//Input voltage high enough to set output low
				{
					mState = false;
					_out = LogicOffLevel;
				} else {
					_out = LogicOnLevel;
				}
			} else {//Output is low
				if (NodeVolts[0] < LowerTrigger)//Input voltage low enough to set output high
				{
					mState = true;
					_out = LogicOnLevel;
				} else {
					_out = LogicOffLevel;
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
		#endregion
	}
}
