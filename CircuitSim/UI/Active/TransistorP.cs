using System.Drawing;

namespace Circuit.UI.Active {
    class TransistorP : Transistor {
        public TransistorP(Point pos) : base(pos, true) { }

        public override DUMP_ID Shortcut { get { return DUMP_ID.BIPOLER_PNP; } }
    }
}
