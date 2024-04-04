using Circuit.Elements.Passive;

namespace Circuit.Elements.Input {
	class ElmLogicInput : ElmSwitch {
		public double VHigh = 5;
		public double VLow = 0;

		public override int VoltageSourceCount { get { return 1; } }

		public override int TermCount { get { return 1; } }

		public override double GetVoltageDiff() {
			return NodeVolts[0];
		}

		public override bool HasGroundConnection(int nodeIndex) { return true; }

		public override void Stamp() {
			double v = 0 != Position ? VHigh : VLow;
			StampVoltageSource(0, NodeId[0], mVoltSource, v);
		}

		public override double GetCurrent(int n) {
			return -Current;
		}

		public override void SetCurrent(int vs, double c) { Current = -c; }
	}
}
