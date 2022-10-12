using System.Drawing;

namespace Circuit.UI.Active {
    class MosfetN : Mosfet {
        public MosfetN(Point pos) : base(pos, false) { }

        public override DUMP_ID Shortcut { get { return DUMP_ID.NMOS; } }
    }
}
