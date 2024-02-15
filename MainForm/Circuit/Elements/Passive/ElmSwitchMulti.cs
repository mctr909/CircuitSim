namespace Circuit.Elements.Passive {
	class ElmSwitchMulti : ElmSwitch {
		public int ThrowCount = 2;

		public override bool IsWire { get { return true; } }

		public override int VoltageSourceCount { get { return 1; } }

		public override int TermCount { get { return 1 + ThrowCount; } }

		public override bool GetConnection(int n1, int n2) {
			return ComparePair(n1, n2, 0, 1 + Position);
		}

		public override void Stamp() {
			var n0 = Nodes[0] - 1;
			var n1 = Nodes[Position + 1] - 1;
			int vn = CircuitElement.Nodes.Count + mVoltSource - 1;
			CircuitElement.Matrix[vn, n0] -= 1;
			CircuitElement.Matrix[vn, n1] += 1;
			CircuitElement.Matrix[n0, vn] += 1;
			CircuitElement.Matrix[n1, vn] -= 1;
		}

		public override double GetCurrentIntoNode(int n) {
			if (n == 0) {
				return -Current;
			}
			if (n == Position + 1) {
				return Current;
			}
			return 0;
		}

	}
}
