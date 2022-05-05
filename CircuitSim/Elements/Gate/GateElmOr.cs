namespace Circuit.Elements.Gate {
    class GateElmOr : GateElm {
        public GateElmOr() : base() { }

        public GateElmOr(StringTokenizer st) : base(st) { }

        protected override bool calcFunction() {
            bool f = false;
            for (int i = 0; i != InputCount; i++) {
                f |= getInput(i);
            }
            return f;
        }
    }
}
