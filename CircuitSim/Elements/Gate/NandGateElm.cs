using System.Drawing;

namespace Circuit.Elements.Gate {
    class NandGateElm : AndGateElm {
        public NandGateElm(Point pos) : base(pos) { }

        public NandGateElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) { }

        public override DUMP_ID DumpType { get { return DUMP_ID.NAND_GATE; } }

        protected override bool isInverting() { return true; }

        protected override string getGateName() { return "NAND gate"; }
    }
}
