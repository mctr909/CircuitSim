namespace Circuit.Elements.Measure {
	class ElmLogicOutput : BaseElement {
		public bool NeedsPullDown;

		public override int TermCount { get { return 1; } }

		public override double GetVoltageDiff() {
			return NodeVolts[0];
		}

		public override void Stamp() {
			if (NeedsPullDown) {
				StampResistor(NodeId[0], 0, 1e6);
			}
		}
	}
}
