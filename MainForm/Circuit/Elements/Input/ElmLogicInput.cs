using Circuit.Elements.Passive;

namespace Circuit.Elements.Input {
	class ElmLogicInput : ElmSwitch {
		public double VHigh;
		public double VLow;

		public ElmLogicInput() {
			VHigh = 5;
			VLow = 0;
		}

		public ElmLogicInput(StringTokenizer st) {
			VHigh = st.nextTokenDouble(5);
			VLow = st.nextTokenDouble(0);
		}

		public override int VoltageSourceCount { get { return 1; } }

		public override int TermCount { get { return 1; } }

		public override double VoltageDiff { get { return Volts[0]; } }

		public override bool HasGroundConnection(int n1) { return true; }

		public override double GetCurrentIntoNode(int n) {
			return -Current;
		}

		public override void SetCurrent(int vs, double c) { Current = -c; }

		public override void Stamp() {
			double v = 0 != Position ? VHigh : VLow;
			Circuit.StampVoltageSource(0, Nodes[0], mVoltSource, v);
		}
	}
}
