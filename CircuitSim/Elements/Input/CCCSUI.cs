﻿using System.Drawing;

namespace Circuit.Elements.Input {
    class CCCSUI : VCCSUI {
        public CCCSUI(Point pos) : base(pos, 0) {
            Elm = new CCCSElm(this);
            DumpInfo.ReferenceName = "CCCS";
        }

        public CCCSUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new CCCSElm(this, st);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.CCCS; } }

        public override ElementInfo GetElementInfo(int r, int c) {
            /* can't set number of inputs */
            if (r == 1) {
                return null;
            }
            return base.GetElementInfo(r, c);
        }
    }
}
