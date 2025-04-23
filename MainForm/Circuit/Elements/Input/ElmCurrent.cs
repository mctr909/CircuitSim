namespace Circuit.Elements.Input {
	class ElmCurrent : BaseElement {
		public double CurrentValue = 0.01;

		public override double VoltageDiff {
			get { return V[1] - V[0]; }
		}
	}
}
