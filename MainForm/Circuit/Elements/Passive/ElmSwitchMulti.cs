﻿namespace Circuit.Elements.Passive {
	class ElmSwitchMulti : ElmSwitch {
		public int ThrowCount = 2;

		public override bool IsWire { get { return true; } }

		public override int VoltageSourceCount { get { return 1; } }

		public override int TermCount { get { return 1 + ThrowCount; } }

		public override bool HasConnection(int n1, int n2) {
			return ComparePair(n1, n2, 0, 1 + Position);
		}

		public override void Stamp() {
			StampVoltageSource(NodeId[0], NodeId[Position + 1], mVoltSource, 0);
		}

		public override double GetCurrent(int n) {
			if (n == 0) {
				return -Current;
			}
			if (n == Position + 1) {
				return Current;
			}
			return 0;
		}

	}
}
