using System.Drawing;

using Circuit.Elements.Custom;

namespace Circuit.UI.Custom {
    class Graphic : BaseUI {
        public Graphic(Point pos) : base(pos) {
            Elm = new ElmGraphic();
        }

        public Graphic(Point a, Point b, int flags) : base(a, b, flags) {
            Elm = new ElmGraphic();
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.INVALID; } }
    }
}
