namespace Circuit.Elements {
    class GraphicElm : CircuitElm {
        public GraphicElm(int xx, int yy) : base(xx, yy) { }

        public GraphicElm(int xa, int ya, int xb, int yb, int flags) : base(xa, ya, xb, yb, flags) { }

        public override int PostCount { get { return 0; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.GRAPHIC; } }

        protected override string dump() { return ""; }
    }
}
