namespace Circuit.Elements.Output {
    class ScopeElmE : BaseElement {
        private Scope mScope;

        public ScopeElmE(Scope scope) { mScope = scope; }

        public override int CirPostCount { get { return 0; } }

        public override void CirReset() {
            base.CirReset();
            mScope.ResetGraph(true);
        }
    }
}
