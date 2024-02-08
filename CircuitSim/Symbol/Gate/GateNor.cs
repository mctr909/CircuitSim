using System.Drawing;

namespace Circuit.Symbol.Gate {
	class GateNor : GateOr {
		public GateNor(Point pos) : base(pos) {
			mElm.IsInverting = true;
		}

		public GateNor(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
			mElm.IsInverting = true;
		}

		public override DUMP_ID DumpId { get { return DUMP_ID.NOR_GATE; } }

		protected override string gateName { get { return "NOR gate"; } }
	}
}
