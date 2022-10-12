using System.Drawing;

namespace Circuit.UI.Active {
    class TransistorN : Transistor {
        public TransistorN(Point pos) : base(pos, false) { }

        public override DUMP_ID Shortcut { get { return DUMP_ID.BIPOLER_NPN; } }
    }
}
