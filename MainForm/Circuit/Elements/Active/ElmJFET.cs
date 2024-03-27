namespace Circuit.Elements.Active {
	class ElmJFET : ElmFET {
		int mDiodeNodesA;
		int mDiodeNodesB;
		double mDiodeLastVdiff = 0.0;
		double mGateCurrent;

		#region [method(Analyze)]
		public override void Reset() {
			base.Reset();
			mDiodeLastVdiff = 0.0;
			mGateCurrent = 0.0;
		}

		public override void Stamp() {
			base.Stamp();
			if (Nch < 0) {
				mDiodeNodesA = NodeIndex[IdxS];
				mDiodeNodesB = NodeIndex[IdxG];
			} else {
				mDiodeNodesA = NodeIndex[IdxG];
				mDiodeNodesB = NodeIndex[IdxS];
			}
			CircuitElement.StampNonLinear(mDiodeNodesA);
			CircuitElement.StampNonLinear(mDiodeNodesB);
		}
		#endregion

		#region [method(Circuit)]
		public override void DoIteration() {
			base.DoIteration();
			DiodeDoIteration(Nch * (Volts[IdxG] - Volts[IdxS]), ref mDiodeLastVdiff, mDiodeNodesA, mDiodeNodesB);
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

		public override void SetCurrent(int n, double c) {
			mGateCurrent = Nch * DiodeCalculateCurrent(Nch * (Volts[IdxG] - Volts[IdxS]));
		}
		#endregion
	}
}
