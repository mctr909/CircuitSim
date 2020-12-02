namespace Circuit.Elements {
    class NandGateElm : AndGateElm {
        public NandGateElm(int xx, int yy) : base(xx, yy) { }

        public NandGateElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f, st) { }

        protected override DUMP_ID getDumpType() { return DUMP_ID.NAND_GATE; }

        protected override bool isInverting() { return true; }

        protected override string getGateName() { return "NAND gate"; }

        public override DUMP_ID getShortcut() { return DUMP_ID.NAND_GATE; }
    }
}
