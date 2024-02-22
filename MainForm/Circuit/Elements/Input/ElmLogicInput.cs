using Circuit.Elements.Passive;

namespace Circuit.Elements.Input {
	class ElmLogicInput : ElmSwitch {
		public double VHigh = 5;
		public double VLow = 0;

		public override int VoltageSourceCount { get { return 1; } }

		public override int TermCount { get { return 1; } }

		public override double VoltageDiff { get { return Volts[0]; } }

		public override bool HasGroundConnection(int n1) { return true; }

		public override void Stamp() {
			double v = 0 != Position ? VHigh : VLow;
			CircuitElement.StampVoltageSource(0, Nodes[0], mVoltSource, v);
		}

		public override double GetCurrentIntoNode(int n) {
			return -Current;
		}

		public override void SetCurrent(int vs, double c) { Current = -c; }
	}
}
