namespace Circuit.Elements.Active {
	class ElmJFET : ElmFET {
		int[] mDiodeNodes = new int[2];
		double mDiodeLastVdiff = 0.0;
		double mGateCurrent;

		public override void Reset() {
			base.Reset();
			mDiodeLastVdiff = 0.0;
			mGateCurrent = 0.0;
		}

		public override void Stamp() {
			base.Stamp();
			if (Nch < 0) {
				mDiodeNodes[0] = Nodes[IdxS];
				mDiodeNodes[1] = Nodes[IdxG];
			} else {
				mDiodeNodes[0] = Nodes[IdxG];
				mDiodeNodes[1] = Nodes[IdxS];
			}
			CircuitElement.StampNonLinear(mDiodeNodes[0]);
			CircuitElement.StampNonLinear(mDiodeNodes[1]);
		}

		public override double GetCurrentIntoNode(int n) {
			if (n == 0) {
				return -mGateCurrent;
			}
			if (n == 1) {
				return mGateCurrent + Current;
			}
			return -Current;
		}

		public override void DoIteration() {
			base.DoIteration();
			DiodeDoIteration(Nch * (Volts[IdxG] - Volts[IdxS]), ref mDiodeLastVdiff, mDiodeNodes[0], mDiodeNodes[1]);
		}

		public override void SetCurrent(int n, double c) {
			mGateCurrent = Nch * DiodeCalculateCurrent(Nch * (Volts[IdxG] - Volts[IdxS]));
		}
	}
}
