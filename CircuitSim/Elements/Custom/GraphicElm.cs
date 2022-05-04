using System.Drawing;

namespace Circuit.Elements.Custom {
    class GraphicElm : CircuitElm {
        public GraphicElm(Point pos) : base(pos) { }

        public GraphicElm(Point a, Point b, int flags) : base(a, b, flags) { }

        public override int CirPostCount { get { return 0; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.INVALID; } }

        protected override string dump() { return ""; }
    }
}
