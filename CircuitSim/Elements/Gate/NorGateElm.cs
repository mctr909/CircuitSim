using System.Drawing;

namespace Circuit.Elements.Gate {
    class NorGateElm : OrGateElm {
        public NorGateElm(Point pos) : base(pos) {
            ((GateElmE)CirElm).IsInverting = true;
        }

        public NorGateElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
            ((GateElmE)CirElm).IsInverting = true;
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.NOR_GATE; } }

        protected override string gateName { get { return "NOR gate"; } }
    }
}
