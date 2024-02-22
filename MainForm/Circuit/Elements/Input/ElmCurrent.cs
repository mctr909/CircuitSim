namespace Circuit.Elements.Input {
	class ElmCurrent : BaseElement {
		public double CurrentValue = 0.01;

		public override int TermCount { get { return 2; } }

		public override double VoltageDiff { get { return Volts[1] - Volts[0]; } }

		public void StampCurrentSource(bool broken) {
			if (broken) {
				/* no current path; stamping a current source would cause a matrix error. */
				CircuitElement.StampResistor(Nodes[0], Nodes[1], 1e8);
				Current = 0;
			} else {
				/* ok to stamp a current source */
				CircuitElement.StampCurrentSource(Nodes[0], Nodes[1], CurrentValue);
				Current = CurrentValue;
			}
		}
	}
}
