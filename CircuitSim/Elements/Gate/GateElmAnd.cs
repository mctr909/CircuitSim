namespace Circuit.Elements.Gate {
    class GateElmAnd : GateElm {
        public GateElmAnd() : base() { }

        public GateElmAnd(StringTokenizer st) : base(st) { }

        protected override bool calcFunction() {
            bool f = true;
            for (int i = 0; i != InputCount; i++) {
                f &= getInput(i);
            }
            return f;
        }
    }
}
