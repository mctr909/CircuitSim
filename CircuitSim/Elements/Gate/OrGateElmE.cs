namespace Circuit.Elements.Gate {
    class OrGateElmE : GateElmE {
        public OrGateElmE() : base() { }

        public OrGateElmE(StringTokenizer st) : base(st) { }

        protected override bool calcFunction() {
            bool f = false;
            for (int i = 0; i != InputCount; i++) {
                f |= getInput(i);
            }
            return f;
        }
    }
}
