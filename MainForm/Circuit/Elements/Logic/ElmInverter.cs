namespace Circuit.Elements.Logic {
	class ElmInverter : BaseElement {
		public double SlewRate = 0.5; /* V/ns */
		public double HighVoltage = ElmGate.LastHighVoltage;

		double mLastOutputVoltage;

		public override int TermCount { get { return 2; } }

		public override int VoltageSourceCount { get { return 1; } }

		public override double VoltageDiff() {
			return Volts[0];
		}

		#region [method(Analyze)]
		/* there is no current path through the inverter input,
         * but there is an indirect path through the output to ground. */
		public override bool HasConnection(int n1, int n2) { return false; }

		public override bool HasGroundConnection(int n1) { return n1 == 1; }

		public override void Stamp() {
			CircuitElement.StampVoltageSource(0, NodeIndex[1], mVoltSource);
		}
		#endregion

		#region [method(Circuit)]
		public override void PrepareIteration() {
			mLastOutputVoltage = Volts[1];
		}

		public override void DoIteration() {
			double v = Volts[0] > HighVoltage * .5 ? 0 : HighVoltage;
			double maxStep = SlewRate * CircuitElement.delta_time * 1e9;
			v = Math.Max(Math.Min(mLastOutputVoltage + maxStep, v), mLastOutputVoltage - maxStep);
			CircuitElement.UpdateVoltageSource(mVoltSource, v);
		}

		public override double GetCurrentIntoNode(int n) {
			if (n == 1) {
				return Current;
			}
			return 0;
		}
		#endregion
	}
}
