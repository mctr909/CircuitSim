using System.Drawing;

namespace Circuit.Elements.Custom {
    class GraphicElm : CircuitElm {
        public GraphicElm(Point pos) : base(pos) {
            CirElm = new BaseElement();
        }

        public GraphicElm(Point a, Point b, int flags) : base(a, b, flags) {
            CirElm = new BaseElement();
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.INVALID; } }

        protected override string dump() { return ""; }
    }
}
