namespace Circuit.Elements.Passive {
	class ElmInductor : BaseElement {
		double mCompResistance;
		double mCurSourceValue;

		public double Inductance = 1e-4;

		public override int TermCount { get { return 2; } }

		public override void Reset() {
			Current = Volts[0] = Volts[1] = mCurSourceValue = 0;
		}

		public override void Stamp() {
			mCompResistance = 2 * Inductance / CircuitElement.delta_time;
			CircuitElement.StampResistor(Nodes[0], Nodes[1], mCompResistance);
			CircuitElement.StampRightSide(Nodes[0]);
			CircuitElement.StampRightSide(Nodes[1]);
		}

		public override void PrepareIteration() {
			mCurSourceValue = (Volts[0] - Volts[1]) / mCompResistance + Current;
		}

		public override void DoIteration() {
			var r = CircuitElement.row_info[Nodes[0] - 1].row;
			CircuitElement.right_side[r] -= mCurSourceValue;
			r = CircuitElement.row_info[Nodes[1] - 1].row;
			CircuitElement.right_side[r] += mCurSourceValue;
		}

		public override void SetVoltage(int n, double c) {
			Volts[n] = c;
			Current = (Volts[0] - Volts[1]) / mCompResistance + mCurSourceValue;
		}
	}
}
