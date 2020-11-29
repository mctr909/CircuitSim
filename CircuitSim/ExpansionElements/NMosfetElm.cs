namespace Circuit.Elements {
    class NMosfetElm : MosfetElm {
        public NMosfetElm(int xx, int yy) : base(xx, yy, false) { }

        public override DUMP_ID getShortcut() { return DUMP_ID.NMOS; }
    }
}
