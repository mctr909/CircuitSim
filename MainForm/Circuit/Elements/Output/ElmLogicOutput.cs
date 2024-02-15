﻿namespace Circuit.Elements.Output {
	class ElmLogicOutput : BaseElement {
		public double Threshold;
		public string Value;
		public bool NeedsPullDown;

		public ElmLogicOutput() : base() {
			Threshold = 2.5;
		}

		public override int TermCount { get { return 1; } }

		public override double VoltageDiff { get { return Volts[0]; } }

		public override void Stamp() {
			if (NeedsPullDown) {
				CircuitElement.StampResistor(Nodes[0], 0, 1e6);
			}
		}
	}
}
