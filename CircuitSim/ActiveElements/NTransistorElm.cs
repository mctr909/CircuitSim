using System.Drawing;

namespace Circuit.ActiveElements {
    class NTransistorElm : TransistorElm {
        public NTransistorElm(Point pos) : base(pos, false) { }

        public override DUMP_ID Shortcut { get { return DUMP_ID.BIPOLER_NPN; } }
    }
}
