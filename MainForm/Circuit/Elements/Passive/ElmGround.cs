namespace Circuit.Elements.Passive {
	class ElmGround : BaseElement {
		public override int VoltageSourceCount { get { return 1; } }

		public override int TermCount { get { return 1; } }

		public override double GetVoltageDiff() {
			return 0;
		}

		#region [method(Analyze)]
		public override bool HasGroundConnection(int nodeIndex) { return true; }

		public override void Stamp() {
			StampVoltageSource(0, NodeId[0], mVoltSource, 0);
		}
		#endregion

		#region [method(Circuit)]
		public override double GetCurrent(int n) { return -Current; }

		public override void SetCurrent(int x, double c) { Current = -c; }
		#endregion
	}
}
