namespace Circuit.Elements.Measure {
	class ElmScope : BaseElement {
		private ScopePlot mScope;

		public override int TermCount { get { return 0; } }

		public ElmScope(ScopePlot scope) { mScope = scope; }

		public override void reset() {
			base.reset();
			mScope.ResetGraph(true);
		}
	}
}
