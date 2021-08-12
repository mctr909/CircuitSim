using System.Drawing;

namespace Circuit.Elements.Active {
    class NMosfetElm : MosfetElm {
        public NMosfetElm(Point pos) : base(pos, false) { }

        public override DUMP_ID Shortcut { get { return DUMP_ID.NMOS; } }
    }
}
