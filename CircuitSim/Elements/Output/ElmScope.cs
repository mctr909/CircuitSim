using Circuit.UI.Output;

namespace Circuit.Elements.Output {
    class ElmScope : BaseElement {
        private Scope.Property mScope;

        public ElmScope(Scope.Property scope) { mScope = scope; }

        public override int PostCount { get { return 0; } }

        public override void Reset() {
            base.Reset();
            mScope.ResetGraph(true);
        }
    }
}
