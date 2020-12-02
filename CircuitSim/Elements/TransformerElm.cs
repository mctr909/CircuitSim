using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements {
    class TransformerElm : CircuitElm {
        public const int FLAG_REVERSE = 4;

        public TransformerElm(int xx, int yy) : base(xx, yy) {
        }

        public TransformerElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
        }

        protected override string dump() {
            return "";
        }

        protected override DUMP_ID getDumpType() { return DUMP_ID.TRANSFORMER; }

        public override DUMP_ID getShortcut() { return DUMP_ID.TRANSFORMER; }
    }
}
