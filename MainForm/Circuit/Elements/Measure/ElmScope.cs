namespace Circuit.Elements.Measure {
	class ElmScope : BaseElement {
		public ScopePlot mScope;

		public override int TermCount { get { return 0; } }

		public ElmScope(ScopePlot scope) { mScope = scope; }
	}
}
