namespace Circuit.Elements.Logic {
	class ElmInverter : BaseElement {
		public double SlewRate = 0.5; /* V/ns */
		public double HighVoltage = ElmGate.LastHighVoltage;

		double mLastOutputVoltage;

		public override double VoltageDiff { get { return V[0]; } }

		protected override void StartIteration() {
			mLastOutputVoltage = V[1];
		}

		protected override void DoIteration() {
			double v = V[0] > HighVoltage * .5 ? 0 : HighVoltage;
			double maxStep = SlewRate * CircuitState.DeltaTime * 1e9;
			v = Math.Max(Math.Min(mLastOutputVoltage + maxStep, v), mLastOutputVoltage - maxStep);
			UpdateVoltageSource(VoltSource, v);
		}

		protected override double GetCurrent(int n) { return (n == 1) ? I[0] : 0; }
	}
}
