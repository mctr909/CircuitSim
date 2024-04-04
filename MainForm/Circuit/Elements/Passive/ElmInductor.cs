namespace Circuit.Elements.Passive {
	class ElmInductor : BaseElement {
		double mCompResistance;
		double mCurSourceValue;

		public double Inductance = 1e-4;

		public override int TermCount { get { return 2; } }

		#region [method(Analyze)]
		public override void Reset() {
			Current = NodeVolts[0] = NodeVolts[1] = mCurSourceValue = 0;
		}

		public override void Stamp() {
			mCompResistance = 2 * Inductance / CircuitState.DeltaTime;
			StampResistor(NodeId[0], NodeId[1], mCompResistance);
			StampRightSide(NodeId[0]);
			StampRightSide(NodeId[1]);
		}
		#endregion

		#region [method(Circuit)]
		public override void PrepareIteration() {
			mCurSourceValue = (NodeVolts[0] - NodeVolts[1]) / mCompResistance + Current;
		}

		public override void DoIteration() {
			UpdateCurrent(NodeId[0], NodeId[1], mCurSourceValue);
		}

		public override void SetVoltage(int nodeIndex, double v) {
			NodeVolts[nodeIndex] = v;
			Current = (NodeVolts[0] - NodeVolts[1]) / mCompResistance + mCurSourceValue;
		}
		#endregion
	}
}
