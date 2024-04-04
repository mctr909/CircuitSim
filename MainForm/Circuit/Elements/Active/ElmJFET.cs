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
				mDiodeNodesA = NodeId[IdxS];
				mDiodeNodesB = NodeId[IdxG];
			} else {
				mDiodeNodesA = NodeId[IdxG];
				mDiodeNodesB = NodeId[IdxS];
			}
			StampNonLinear(mDiodeNodesA);
			StampNonLinear(mDiodeNodesB);
		}
		#endregion

		#region [method(Circuit)]
		public override void DoIteration() {
			base.DoIteration();
			DiodeDoIteration(Nch * (NodeVolts[IdxG] - NodeVolts[IdxS]), ref mDiodeLastVdiff, mDiodeNodesA, mDiodeNodesB);
		}

		public override double GetCurrent(int n) {
			if (n == 0) {
				return -mGateCurrent;
			}
			if (n == 1) {
				return mGateCurrent + Current;
			}
			return -Current;
		}

		public override void SetCurrent(int n, double c) {
			mGateCurrent = Nch * DiodeCalculateCurrent(Nch * (NodeVolts[IdxG] - NodeVolts[IdxS]));
		}
		#endregion
	}
}
