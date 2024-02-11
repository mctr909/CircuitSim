using System.Drawing;

using Circuit.Elements.Input;

namespace Circuit.Symbol.Input {
    class CCCS : VCCS {
        public CCCS(Point pos) : base(pos, 0) {
            mElm = new ElmCCCS(this);
            ReferenceName = "CCCS";
        }

        public CCCS(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            mElm = new ElmCCCS(this, st);
        }

        public override DUMP_ID DumpId { get { return DUMP_ID.CCCS; } }

        public override ElementInfo GetElementInfo(int r, int c) {
            /* can't set number of inputs */
            if (r == 1) {
                return null;
            }
            return base.GetElementInfo(r, c);
        }
    }
}
