using System.Drawing;

namespace Circuit.Elements.Gate {
    class GateUINand : GateUIAnd {
        public GateUINand(Point pos) : base(pos) {
            ((GateElm)CirElm).IsInverting = true;
        }

        public GateUINand(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
            ((GateElm)CirElm).IsInverting = true;
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.NAND_GATE; } }

        protected override string gateName { get { return "NAND gate"; } }
    }
}
