namespace Circuit.Elements.Active {
	class ElmAnalogSwitch : BaseElement {
		public double Ron = 100;
		public double Roff = 1e8;
		public bool Invert;
		public bool IsOpen;

		double mResistance;

		public override int TermCount { get { return 3; } }

		public override bool HasConnection(int n1, int n2) { return !(n1 == 2 || n2 == 2); }

		public override void Stamp() {
			StampNonLinear(NodeId[0]);
			StampNonLinear(NodeId[1]);
		}

		#region [method(Circuit)]
		public override void DoIteration() {
			IsOpen = NodeVolts[2] < 2.5;
			if (Invert) {
				IsOpen = !IsOpen;
			}
			mResistance = IsOpen ? Roff : Ron;
			UpdateConductance(NodeId[0], NodeId[1], 1.0 / mResistance);
		}

		public override double GetCurrent(int n) {
			if (n == 0) {
				return -Current;
			}
			if (n == 2) {
				return 0;
			}
			return Current;
		}

		public override void SetVoltage(int nodeIndex, double v) {
			NodeVolts[nodeIndex] = v;
			Current = (NodeVolts[0] - NodeVolts[1]) / mResistance;
		}
		#endregion
	}
}
