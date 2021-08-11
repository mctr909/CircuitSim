using System.Drawing;

namespace Circuit.Elements {
    class NandGateElm : AndGateElm {
        public NandGateElm(Point pos) : base(pos) { }

        public NandGateElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f, st) { }

        public override DUMP_ID DumpType { get { return DUMP_ID.NAND_GATE; } }

        protected override bool isInverting() { return true; }

        protected override string getGateName() { return "NAND gate"; }
    }
}
