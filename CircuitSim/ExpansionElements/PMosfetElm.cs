namespace Circuit.Elements {
    class PMosfetElm : MosfetElm {
        public PMosfetElm(int xx, int yy) : base(xx, yy, true) { }

        public override DUMP_ID Shortcut { get { return DUMP_ID.PMOS; } }
    }
}
