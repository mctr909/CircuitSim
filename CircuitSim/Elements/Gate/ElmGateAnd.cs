namespace Circuit.Elements.Gate {
    class ElmGateAnd : ElmGate {
        public ElmGateAnd() : base() { }

        public ElmGateAnd(StringTokenizer st) : base(st) { }

        protected override bool calcFunction() {
            bool f = true;
            for (int i = 0; i != InputCount; i++) {
                f &= getInput(i);
            }
            return f;
        }
    }
}
