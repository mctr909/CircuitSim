namespace Circuit.Elements.Output {
    class ScopeElmE : BaseElement {
        private Scope mScope;

        public ScopeElmE(Scope scope) { mScope = scope; }

        public override int PostCount { get { return 0; } }

        public override void Reset() {
            base.Reset();
            mScope.ResetGraph(true);
        }
    }
}
