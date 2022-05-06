using System.Drawing;

namespace Circuit.Elements.Gate {
    class GateUINor : GateUIOr {
        public GateUINor(Point pos) : base(pos) {
            ((GateElm)Elm).IsInverting = true;
        }

        public GateUINor(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
            ((GateElm)Elm).IsInverting = true;
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.NOR_GATE; } }

        protected override string gateName { get { return "NOR gate"; } }
    }
}
