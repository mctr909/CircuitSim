namespace Circuit.Elements.Passive {
	class ElmTransformer : BaseElement {
		public const int PRI_T = 0;
		public const int SEC_T = 1;
		public const int PRI_B = 2;
		public const int SEC_B = 3;
		public const int CS_PRI = 0;
		public const int CS_SEC = 1;
		public const int CUR_PRI = 2;
		public const int CUR_SEC = 3;

		public override int TermCount { get { return 4; } }

		protected override void DoIteration() {
			var priT = Nodes[PRI_T] - 1;
			var priB = Nodes[PRI_B] - 1;
			var secT = Nodes[SEC_T] - 1;
			var secB = Nodes[SEC_B] - 1;
			priT = NODE_INFOS[priT].Row;
			priB = NODE_INFOS[priB].Row;
			secT = NODE_INFOS[secT].Row;
			secB = NODE_INFOS[secB].Row;
			RIGHTSIDE[priT] -= I[CS_PRI];
			RIGHTSIDE[priB] += I[CS_PRI];
			RIGHTSIDE[secT] -= I[CS_SEC];
			RIGHTSIDE[secB] += I[CS_SEC];
		}

		protected override void StartIteration() {
			var priV = V[PRI_T] - V[PRI_B];
			var secV = V[SEC_T] - V[SEC_B];
			I[CS_PRI] = I[CUR_PRI] + priV * Para[PRI_T] + secV * Para[SEC_T];
			I[CS_SEC] = I[CUR_SEC] + priV * Para[PRI_B] + secV * Para[SEC_B];
		}

		protected override double GetCurrent(int n) { return (n < 2) ? -I[n] : I[n - 2]; }

		public override void SetVoltage(int n, double v) {
			V[n] = v;
			var priV = V[PRI_T] - V[PRI_B];
			var secV = V[SEC_T] - V[SEC_B];
			I[CUR_PRI] = I[CS_PRI] + priV * Para[PRI_T] + secV * Para[SEC_T];
			I[CUR_SEC] = I[CS_SEC] + priV * Para[PRI_B] + secV * Para[SEC_B];
		}
	}
}
