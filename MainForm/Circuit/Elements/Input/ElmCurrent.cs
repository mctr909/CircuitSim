namespace Circuit.Elements.Input {
	class ElmCurrent : BaseElement {
		public double CurrentValue = 0.01;

		public override int TermCount { get { return 2; } }

		public override double VoltageDiff() {
			return Volts[1] - Volts[0];
		}

		public void StampCurrentSource(bool broken) {
			if (broken) {
				/* no current path; stamping a current source would cause a matrix error. */
				CircuitElement.StampResistor(NodeIndex[0], NodeIndex[1], 1e8);
				Current = 0;
			} else {
				/* ok to stamp a current source */
				CircuitElement.StampCurrentSource(NodeIndex[0], NodeIndex[1], CurrentValue);
				Current = CurrentValue;
			}
		}
	}
}
