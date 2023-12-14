namespace Circuit.Elements.Passive {
	class ElmGround : BaseElement {
		public override int VoltageSourceCount { get { return 1; } }

		public override int TermCount { get { return 1; } }

		public override double VoltageDiff { get { return 0; } }

		public override bool HasGroundConnection(int n1) { return true; }

		public override void Stamp() {
			var nv = Circuit.Nodes.Count + mVoltSource - 1;
			var n0 = Nodes[0] - 1;
			Circuit.Matrix[nv, n0] += 1;
			Circuit.Matrix[n0, nv] -= 1;
		}

		public override double GetCurrentIntoNode(int n) { return -Current; }

		public override void SetCurrent(int x, double c) { Current = -c; }
	}
}
