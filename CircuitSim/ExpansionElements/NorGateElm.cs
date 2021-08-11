using System.Drawing;

namespace Circuit.Elements {
    class NorGateElm : OrGateElm {
        public NorGateElm(Point pos) : base(pos) { }

        public NorGateElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) { }

        public override DUMP_ID DumpType { get { return DUMP_ID.NOR_GATE; } }

        protected override string getGateName() { return "NOR gate"; }

        protected override bool isInverting() { return true; }
    }
}
