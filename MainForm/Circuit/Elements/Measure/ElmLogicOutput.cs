namespace Circuit.Elements.Measure {
	class ElmLogicOutput : BaseElement {
		public bool NeedsPullDown;

		public override int TermCount { get { return 1; } }

		public override double VoltageDiff { get { return V[0]; } }
	}
}
