namespace Circuit.Elements.Logic {
	class ElmTriState : BaseElement {
		public double Ron = 0.1;
		public double Roff = 1e10;
		public bool Open;

		double mResistance;

		public override int TermCount { get { return 3; } }

		public override int InternalNodeCount { get { return 1; } }

		public override int VoltageSourceCount { get { return 1; } }

		#region [method(Analyze)]
		/* there is no current path through the input, but there
         * is an indirect path through the output to ground. */
		public override bool HasConnection(int n1, int n2) { return false; }

		public override bool HasGroundConnection(int nodeIndex) { return nodeIndex == 1; }

		public override void Stamp() {
			StampVoltageSource(0, NodeId[3], mVoltSource);
			StampNonLinear(NodeId[3]);
			StampNonLinear(NodeId[1]);
		}
		#endregion

		#region [method(Circuit)]
		public override void DoIteration() {
			Open = NodeVolts[2] < 2.5;
			mResistance = Open ? Roff : Ron;
			UpdateConductance(NodeId[3], NodeId[1], 1.0 / mResistance);
			UpdateVoltage(mVoltSource, NodeVolts[0] > 2.5 ? 5 : 0);
		}

		public override double GetCurrent(int n) {
			if (n == 1) {
				return Current;
			}
			return 0;
		}

		public override void SetVoltage(int nodeIndex, double v) {
			NodeVolts[nodeIndex] = v;
			Current = (NodeVolts[0] - NodeVolts[1]) / mResistance;
		}
		#endregion
	}
}
