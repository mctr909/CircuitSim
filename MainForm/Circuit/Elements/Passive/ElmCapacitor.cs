namespace Circuit.Elements.Passive {
	class ElmCapacitor : BaseElement {
		double mCompResistance;
		double mCurSourceValue;

		public double Capacitance = 1e-5;
		public double VoltDiff = 1e-3;

		public override int TermCount { get { return 2; } }

		#region [method(Analyze)]
		public override void Reset() {
			base.Reset();
			Current = mCurSourceValue = 0;
			VoltDiff = 1e-3;
		}

		public override void Shorted() {
			base.Reset();
			VoltDiff = Current = mCurSourceValue = 0;
		}

		public override void Stamp() {
			var n0 = NodeIndex[0] - 1;
			var n1 = NodeIndex[1] - 1;
			if (n0 < 0 || n1 < 0) {
				return;
			}
			mCompResistance = 0.5 * CircuitElement.delta_time / Capacitance;
			CircuitElement.StampResistor(NodeIndex[0], NodeIndex[1], mCompResistance);
			CircuitElement.StampRightSide(NodeIndex[0]);
			CircuitElement.StampRightSide(NodeIndex[1]);
		}
		#endregion

		#region [method(Circuit)]
		public override void PrepareIteration() {
			mCurSourceValue = -VoltDiff / mCompResistance - Current;
		}

		public override void DoIteration() {
			var r = CircuitElement.row_info[NodeIndex[0] - 1].row;
			CircuitElement.right_side[r] -= mCurSourceValue;
			r = CircuitElement.row_info[NodeIndex[1] - 1].row;
			CircuitElement.right_side[r] += mCurSourceValue;
		}

		public override void SetVoltage(int n, double c) {
			Volts[n] = c;
			VoltDiff = Volts[0] - Volts[1];
			Current = VoltDiff / mCompResistance + mCurSourceValue;
		}
		#endregion
	}
}
