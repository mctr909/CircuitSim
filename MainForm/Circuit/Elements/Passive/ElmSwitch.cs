namespace Circuit.Elements.Passive {
	class ElmSwitch : BaseElement {
		public bool Momentary = false;
		public int Position = 0;
		public int PosCount = 2;
		public int Link = 0;

		public override int TermCount { get { return 2; } }
		public override bool IsWire { get { return Position == 0; } }
		public override int VoltageSourceCount { get { return (1 == Position) ? 0 : 1; } }

		public override bool HasConnection(int n1, int n2) { return 0 == Position; }

		public override void Stamp() {
			if (Position == 0) {
				StampVoltageSource(NodeId[0], NodeId[1], mVoltSource, 0);
			}
		}

		public override void SetVoltage(int nodeIndex, double v) {
			NodeVolts[nodeIndex] = v;
			if (Position == 1) {
				Current = 0;
			}
		}
	}
}
