namespace Circuit.Elements {
    class NTransistorElm : TransistorElm {
        public NTransistorElm(int xx, int yy) : base(xx, yy, false) { }

        public override DUMP_ID Shortcut { get { return DUMP_ID.BIPOLER_NPN; } }
    }
}
