﻿namespace Circuit.Elements.Input {
	class ElmCurrent : BaseElement {
		double mCurrentValue;

		public ElmCurrent() {
			mCurrentValue = 0.01;
		}

		public ElmCurrent(double current) {
			mCurrentValue = current;
		}

		public override int TermCount { get { return 2; } }

		public override double VoltageDiff { get { return Volts[1] - Volts[0]; } }

		/* we defer stamping current sources until we can tell if they have a current path or not */
		public void stampCurrentSource(bool broken) {
			if (broken) {
				/* no current path; stamping a current source would cause a matrix error. */
				CircuitElement.StampResistor(Nodes[0], Nodes[1], 1e8);
				Current = 0;
			} else {
				/* ok to stamp a current source */
				CircuitElement.StampCurrentSource(Nodes[0], Nodes[1], mCurrentValue);
				Current = mCurrentValue;
			}
		}
	}
}