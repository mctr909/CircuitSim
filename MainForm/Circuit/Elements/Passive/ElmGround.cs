namespace Circuit.Elements.Passive {
	class ElmGround : BaseElement {
		public override int VoltageSourceCount { get { return 1; } }

		public override int TermCount { get { return 1; } }

		public override double VoltageDiff { get { return 0; } }

		public override bool HasGroundConnection(int n1) { return true; }

		public override void Stamp() {
			CircuitElement.StampVoltageSource(0, Nodes[0], mVoltSource, 0);
		}

		public override double GetCurrentIntoNode(int n) { return -Current; }

		public override void SetCurrent(int x, double c) { Current = -c; }
	}
}
