﻿using Circuit.Elements.Passive;

namespace Circuit.Elements.Input {
	class ElmLogicInput : ElmSwitch {
		public override int TermCount { get { return 1; } }

		public override double VoltageDiff { get { return V[0]; } }

		protected override double GetCurrent(int n) { return -I[0]; }

		protected override void SetCurrent(int n, double i) { I[0] = -i; }
	}
}
