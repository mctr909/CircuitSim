using Circuit.Elements.Logic;

namespace Circuit.Symbol.Gate {
	class GateXor : GateOr {
		public GateXor(Point pos) : base(pos, true) {
			mElm = new ElmGateXor();
		}
		public GateXor(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, true) {
			mElm = new ElmGateXor();
			mElm.InputCount = st.nextTokenInt(mElm.InputCount);
			var lastOutputVoltage = st.nextTokenDouble();
			mElm.HighVoltage = st.nextTokenDouble(5);
			mElm.LastOutput = mElm.HighVoltage * 0.5 < lastOutputVoltage;
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.XOR_GATE; } }

		protected override string gateName { get { return "XOR gate"; } }

		protected override string gateText { get { return "=1"; } }
	}
}
