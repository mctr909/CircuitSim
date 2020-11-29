namespace Circuit.Elements {
    class PTransistorElm : TransistorElm {
        public PTransistorElm(int xx, int yy) : base(xx, yy, true) { }

        public override DUMP_ID getShortcut() { return DUMP_ID.BIPOLER_PNP; }
    }
}
