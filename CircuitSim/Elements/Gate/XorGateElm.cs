using System.Drawing;

namespace Circuit.Elements.Gate {
    class XorGateElm : OrGateElm {
        public XorGateElm(Point pos) : base(pos) { }

        public XorGateElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) { }

        public override DUMP_ID DumpType { get { return DUMP_ID.XOR_GATE; } }

        protected override string getGateName() { return "XOR gate"; }

        protected override string getGateText() { return "=1"; }

        protected override bool calcFunction() {
            bool f = false;
            for (int i = 0; i != mInputCount; i++) {
                f ^= getInput(i);
            }
            return f;
        }
    }
}
