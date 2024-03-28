namespace Circuit.Elements.Input {
	class ElmCurrent : BaseElement {
		public double CurrentValue = 0.01;

		public override int TermCount { get { return 2; } }

		public override double voltage_diff() {
			return volts[1] - volts[0];
		}

		public void StampCurrentSource(bool broken) {
			if (broken) {
				/* no current path; stamping a current source would cause a matrix error. */
				CircuitElement.StampResistor(node_index[0], node_index[1], 1e8);
				current = 0;
			} else {
				/* ok to stamp a current source */
				CircuitElement.StampCurrentSource(node_index[0], node_index[1], CurrentValue);
				current = CurrentValue;
			}
		}
	}
}
