using System.Drawing;

using Circuit.Elements.Gate;

namespace Circuit.UI.Gate {
    class GateNand : GateAnd {
        public GateNand(Point pos) : base(pos) {
            ((ElmGate)Elm).IsInverting = true;
        }

        public GateNand(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
            ((ElmGate)Elm).IsInverting = true;
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.NAND_GATE; } }

        protected override string gateName { get { return "NAND gate"; } }
    }
}
