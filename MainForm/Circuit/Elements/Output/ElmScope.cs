namespace Circuit.Elements.Output {
	class ElmScope : BaseElement {
		private ScopePlot mScope;

		public override int TermCount { get { return 0; } }

		public ElmScope(ScopePlot scope) { mScope = scope; }

		public override void Reset() {
			base.Reset();
			mScope.ResetGraph(true);
		}
	}
}
