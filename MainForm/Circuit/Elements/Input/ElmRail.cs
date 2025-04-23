namespace Circuit.Elements.Input {
	class ElmRail : ElmVoltage {
		public override int TermCount { get { return 1; } }

		public override double VoltageDiff { get { return V[0]; } }
	}
}
