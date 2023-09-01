using System.Drawing;

using Circuit.Elements.Gate;

namespace Circuit.UI.Gate {
    class GateXor : GateOr {
        public GateXor(Point pos) : base(pos, 0) {
            Elm = new ElmGateXor();
        }

        public GateXor(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st, 0) {
            Elm = new ElmGateXor();
        }

        public override DUMP_ID DumpId { get { return DUMP_ID.XOR_GATE; } }

        protected override string gateName { get { return "XOR gate"; } }

        protected override string gateText { get { return "=1"; } }
    }
}
