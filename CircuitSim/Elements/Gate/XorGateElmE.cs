namespace Circuit.Elements.Gate {
    class XorGateElmE : OrGateElmE {
        public XorGateElmE() : base() { }

        protected override bool calcFunction() {
            bool f = false;
            for (int i = 0; i != InputCount; i++) {
                f ^= getInput(i);
            }
            return f;
        }
    }
}
