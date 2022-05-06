using System.Drawing;

namespace Circuit.Elements.Custom {
    class GraphicUI : BaseUI {
        public GraphicUI(Point pos) : base(pos) {
            Elm = new GraphicElm();
        }

        public GraphicUI(Point a, Point b, int flags) : base(a, b, flags) {
            Elm = new GraphicElm();
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.INVALID; } }

        protected override string dump() { return ""; }
    }
}
