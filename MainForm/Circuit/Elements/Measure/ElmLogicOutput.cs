namespace Circuit.Elements.Measure {
	class ElmLogicOutput : BaseElement {
		public bool NeedsPullDown;

		public override int TermCount { get { return 1; } }

		public override double voltage_diff() {
			return volts[0];
		}

		public override void stamp() {
			if (NeedsPullDown) {
				CircuitElement.StampResistor(node_index[0], 0, 1e6);
			}
		}
	}
}
