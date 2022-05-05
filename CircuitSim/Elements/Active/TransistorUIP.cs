using System.Drawing;

namespace Circuit.Elements.Active {
    class TransistorUIP : TransistorUI {
        public TransistorUIP(Point pos) : base(pos, true) { }

        public override DUMP_ID Shortcut { get { return DUMP_ID.BIPOLER_PNP; } }
    }
}
