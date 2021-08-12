using System.Drawing;

namespace Circuit.Elements.Active {
    class PTransistorElm : TransistorElm {
        public PTransistorElm(Point pos) : base(pos, true) { }

        public override DUMP_ID Shortcut { get { return DUMP_ID.BIPOLER_PNP; } }
    }
}
