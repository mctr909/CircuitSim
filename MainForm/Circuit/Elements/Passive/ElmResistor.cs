namespace Circuit.Elements.Passive {
	class ElmResistor : BaseElement {
		public double Resistance = 1000;

		public override int TermCount { get { return 2; } }

		public override void Stamp() {
			StampResistor(NodeId[0], NodeId[1], Resistance);
		}

		public override void SetVoltage(int nodeIndex, double v) {
			NodeVolts[nodeIndex] = v;
			Current = (NodeVolts[0] - NodeVolts[1]) / Resistance;
		}
	}
}
