using System.Drawing;

namespace Circuit.UI.Active {
    class MosfetP : Mosfet {
        public MosfetP(Point pos) : base(pos, true) { }

        public override DUMP_ID Shortcut { get { return DUMP_ID.PMOS; } }
    }
}
