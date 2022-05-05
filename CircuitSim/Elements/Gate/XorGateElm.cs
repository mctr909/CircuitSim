using System.Drawing;

namespace Circuit.Elements.Gate {
    class XorGateElm : OrGateElm {
        public XorGateElm(Point pos) : base(pos, 0) {
            CirElm = new XorGateElmE();
        }

        public XorGateElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st, 0) {
            CirElm = new XorGateElmE();
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.XOR_GATE; } }

        protected override string gateName { get { return "XOR gate"; } }

        protected override string gateText { get { return "=1"; } }
    }
}
