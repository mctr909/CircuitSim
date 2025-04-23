namespace Circuit.Elements.Logic {
	class ElmTriState : BaseElement {
		public double Ron = 0.1;
		public double Roff = 1e10;
		public bool Open;

		double mResistance;

		public override int TermCount { get { return 3; } }

		protected override void DoIteration() {
			Open = V[2] < 2.5;
			mResistance = Open ? Roff : Ron;
			UpdateConductance(Nodes[3], Nodes[1], 1.0 / mResistance);
			UpdateVoltageSource(VoltSource, V[0] > 2.5 ? 5 : 0);
		}

		protected override double GetCurrent(int n) { return (n == 1) ? I[0] : 0; }

		public override void SetVoltage(int n, double v) {
			V[n] = v;
			I[0] = (V[0] - V[1]) / mResistance;
		}
	}
}
