namespace Circuit.Elements.Measure {
	class ElmLogicOutput : BaseElement {
		public bool NeedsPullDown;

		public override int TermCount { get { return 1; } }

		public override double VoltageDiff() {
			return Volts[0];
		}

		public override void Stamp() {
			if (NeedsPullDown) {
				CircuitElement.StampResistor(NodeIndex[0], 0, 1e6);
			}
		}
	}
}
