using Circuit.Common;

namespace Circuit.Elements.Output {
    class ElmScope : BaseElement {
        private ScopePlot mScope;

        public ElmScope(ScopePlot scope) { mScope = scope; }

        public override int TermCount { get { return 0; } }

        public override void Reset() {
            base.Reset();
            mScope.ResetGraph(true);
        }
    }
}
