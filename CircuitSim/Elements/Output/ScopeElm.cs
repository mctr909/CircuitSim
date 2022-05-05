namespace Circuit.Elements.Output {
    class ScopeElm : BaseElement {
        private Scope mScope;

        public ScopeElm(Scope scope) { mScope = scope; }

        public override int PostCount { get { return 0; } }

        public override void Reset() {
            base.Reset();
            mScope.ResetGraph(true);
        }
    }
}
