using System.Drawing;

namespace Circuit.Elements.Active {
    class TransistorUIN : TransistorUI {
        public TransistorUIN(Point pos) : base(pos, false) { }

        public override DUMP_ID Shortcut { get { return DUMP_ID.BIPOLER_NPN; } }
    }
}
