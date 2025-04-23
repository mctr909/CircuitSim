namespace Circuit.Elements.Passive {
	class ElmCapacitor : BaseElement {
		public const int MAX_VOLTAGE = 1;
		public const int MAX_NEGATIVE = 2;

		protected override void DoIteration() {
			var n1 = NODE_INFOS[Nodes[0] - 1].Row;
			var n2 = NODE_INFOS[Nodes[1] - 1].Row;
			RIGHTSIDE[n1] -= I[1];
			RIGHTSIDE[n2] += I[1];
		}

		protected override void StartIteration() {
			I[1] = -I[0] - V[2] / Para[0];
		}

		protected override void FinishIteration() {
			var v = VoltageDiff;
			if (v > Para[MAX_VOLTAGE] || v < -Para[MAX_VOLTAGE]) {
				Broken = true;
				CircuitState.Stopped = true;
			}
			if (v < -Para[MAX_NEGATIVE]) {
				Broken = true;
				CircuitState.Stopped = true;
			}
		}

		public override void SetVoltage(int n, double v) {
			V[n] = v;
			V[2] = V[0] - V[1];
			I[0] = I[1] + V[2] / Para[0];
		}
	}
}
