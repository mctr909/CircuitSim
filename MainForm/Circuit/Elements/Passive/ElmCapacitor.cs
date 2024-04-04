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
			var n0 = NodeId[0] - 1;
			var n1 = NodeId[1] - 1;
			if (n0 < 0 || n1 < 0) {
				return;
			}
			mCompResistance = 0.5 * CircuitState.DeltaTime / Capacitance;
			StampResistor(NodeId[0], NodeId[1], mCompResistance);
			StampRightSide(NodeId[0]);
			StampRightSide(NodeId[1]);
		}
		#endregion

		#region [method(Circuit)]
		public override void PrepareIteration() {
			mCurSourceValue = -VoltDiff / mCompResistance - Current;
		}

		public override void DoIteration() {
			UpdateCurrent(NodeId[0], NodeId[1], mCurSourceValue);
		}

		public override void SetVoltage(int nodeIndex, double v) {
			NodeVolts[nodeIndex] = v;
			VoltDiff = NodeVolts[0] - NodeVolts[1];
			Current = VoltDiff / mCompResistance + mCurSourceValue;
		}
		#endregion
	}
}
