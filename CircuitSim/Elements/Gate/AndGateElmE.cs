namespace Circuit.Elements.Gate {
    class AndGateElmE : GateElmE {
        public AndGateElmE() : base() { }

        public AndGateElmE(StringTokenizer st) : base(st) { }

        protected override bool calcFunction() {
            bool f = true;
            for (int i = 0; i != InputCount; i++) {
                f &= getInput(i);
            }
            return f;
        }
    }
}
