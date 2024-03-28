﻿namespace Circuit.Elements.Passive {
	class ElmWire : BaseElement {
		public bool HasWireInfo; /* used in CirSim to calculate wire currents */

		public override int TermCount { get { return 2; } }

		public override bool IsWire { get { return true; } }

		public override double voltage_diff() {
			return volts[0];
		}
	}
}
