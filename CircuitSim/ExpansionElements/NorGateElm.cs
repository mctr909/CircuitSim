﻿namespace Circuit.Elements {
    class NorGateElm : OrGateElm {
        public NorGateElm(int xx, int yy) : base(xx, yy) { }

        public NorGateElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f, st) { }

        public override DUMP_ID DumpType { get { return DUMP_ID.NOR_GATE; } }

        protected override string getGateName() { return "NOR gate"; }

        protected override bool isInverting() { return true; }
    }
}
