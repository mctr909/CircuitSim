namespace Circuit.Elements {
    class XorGateElm : OrGateElm {
        public XorGateElm(int xx, int yy) : base(xx, yy) { }

        public XorGateElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f, st) { }

        public override DUMP_ID DumpType { get { return DUMP_ID.XOR_GATE; } }

        protected override string getGateName() { return "XOR gate"; }

        protected override string getGateText() { return "=1"; }

        protected override bool calcFunction() {
            bool f = false;
            for (int i = 0; i != inputCount; i++) {
                f ^= getInput(i);
            }
            return f;
        }
    }
}
