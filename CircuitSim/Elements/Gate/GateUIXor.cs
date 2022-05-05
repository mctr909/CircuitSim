using System.Drawing;

namespace Circuit.Elements.Gate {
    class GateUIXor : GateUIOr {
        public GateUIXor(Point pos) : base(pos, 0) {
            CirElm = new GateElmXor();
        }

        public GateUIXor(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st, 0) {
            CirElm = new GateElmXor();
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.XOR_GATE; } }

        protected override string gateName { get { return "XOR gate"; } }

        protected override string gateText { get { return "=1"; } }
    }
}
