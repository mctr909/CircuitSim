namespace Circuit.Elements.Passive {
	class ElmCapacitor : BaseElement {
		double mCompResistance;
		double mCurSourceValue;

		public double Capacitance = 1e-5;
		public double VoltDiff = 0.0;

		public override int TermCount { get { return 2; } }

		public override void Reset() {
			base.Reset();
			Current = mCurSourceValue = 0;
			/* put small charge on caps when reset to start oscillators */
			VoltDiff = 1e-3;
		}

		public override void Shorted() {
			base.Reset();
			VoltDiff = Current = mCurSourceValue = 0;
		}

		public override void Stamp() {
			var n0 = Nodes[0] - 1;
			var n1 = Nodes[1] - 1;
			if (n0 < 0 || n1 < 0) {
				return;
			}
			var g = 2 * Capacitance / CircuitElement.TimeStep;
			mCompResistance = 1.0 / g;
			CircuitElement.Matrix[n0, n0] += g;
			CircuitElement.Matrix[n1, n1] += g;
			CircuitElement.Matrix[n0, n1] -= g;
			CircuitElement.Matrix[n1, n0] -= g;
			CircuitElement.RowInfo[n0].RightChanges = true;
			CircuitElement.RowInfo[n1].RightChanges = true;
		}

		public override void PrepareIteration() {
			mCurSourceValue = -VoltDiff / mCompResistance - Current;
		}

		public override void DoIteration() {
			var r = CircuitElement.RowInfo[Nodes[0] - 1].MapRow;
			CircuitElement.RightSide[r] -= mCurSourceValue;
			r = CircuitElement.RowInfo[Nodes[1] - 1].MapRow;
			CircuitElement.RightSide[r] += mCurSourceValue;
		}

		public override void SetVoltage(int n, double c) {
			Volts[n] = c;
			VoltDiff = Volts[0] - Volts[1];
			Current = VoltDiff / mCompResistance + mCurSourceValue;
		}
	}
}
