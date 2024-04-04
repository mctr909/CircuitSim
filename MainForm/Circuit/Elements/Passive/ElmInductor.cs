namespace Circuit.Elements.Passive {
	class ElmInductor : BaseElement {
		double mCompResistance;
		double mCurSourceValue;

		public double Inductance = 1e-4;

		public override int TermCount { get { return 2; } }

		#region [method(Analyze)]
		public override void Reset() {
			Current = Volts[0] = Volts[1] = mCurSourceValue = 0;
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
			mCurSourceValue = (Volts[0] - Volts[1]) / mCompResistance + Current;
		}

		public override void DoIteration() {
			UpdateCurrent(NodeId[0], NodeId[1], mCurSourceValue);
		}

		public override void SetVoltage(int n, double c) {
			Volts[n] = c;
			Current = (Volts[0] - Volts[1]) / mCompResistance + mCurSourceValue;
		}
		#endregion
	}
}
