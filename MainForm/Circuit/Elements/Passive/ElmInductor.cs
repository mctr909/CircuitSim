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
			var g = CircuitElement.TimeStep / (2 * Inductance);
			var n0 = Nodes[0] - 1;
			var n1 = Nodes[1] - 1;
			if (n0 < 0 || n1 < 0) {
				return;
			}
			mCompResistance = 1.0 / g;
			CircuitElement.Matrix[n0, n0] += g;
			CircuitElement.Matrix[n1, n1] += g;
			CircuitElement.Matrix[n0, n1] -= g;
			CircuitElement.Matrix[n1, n0] -= g;
			CircuitElement.RowInfo[n0].RightChanges = true;
			CircuitElement.RowInfo[n1].RightChanges = true;
		}

		public override void PrepareIteration() {
			mCurSourceValue = (Volts[0] - Volts[1]) / mCompResistance + Current;
		}

		public override void DoIteration() {
			var r = CircuitElement.RowInfo[Nodes[0] - 1].MapRow;
			CircuitElement.RightSide[r] -= mCurSourceValue;
			r = CircuitElement.RowInfo[Nodes[1] - 1].MapRow;
			CircuitElement.RightSide[r] += mCurSourceValue;
		}

		public override void SetVoltage(int n, double c) {
			Volts[n] = c;
			Current = (Volts[0] - Volts[1]) / mCompResistance + mCurSourceValue;
		}
	}
}
