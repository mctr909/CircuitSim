namespace Circuit.Elements.Passive {
	class ElmInductor : BaseElement {
		protected override void DoIteration() {
			var n1 = NODE_INFOS[Nodes[0] - 1].Row;
			var n2 = NODE_INFOS[Nodes[1] - 1].Row;
			RIGHTSIDE[n1] -= I[1];
			RIGHTSIDE[n2] += I[1];
		}

		protected override void StartIteration() {
			I[1] = I[0] + (V[0] - V[1]) / Para[0];
		}

		public override void SetVoltage(int n, double v) {
			V[n] = v;
			I[0] = I[1] + (V[0] - V[1]) / Para[0];
		}
	}
}
