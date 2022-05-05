using System.Drawing;

namespace Circuit.Elements.Active {
    class MosfetUIP : MosfetUI {
        public MosfetUIP(Point pos) : base(pos, true) { }

        public override DUMP_ID Shortcut { get { return DUMP_ID.PMOS; } }
    }
}
