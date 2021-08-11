using System.Drawing;

namespace Circuit.Elements {
    class NTransistorElm : TransistorElm {
        public NTransistorElm(Point pos) : base(pos, false) { }

        public override DUMP_ID Shortcut { get { return DUMP_ID.BIPOLER_NPN; } }
    }
}
