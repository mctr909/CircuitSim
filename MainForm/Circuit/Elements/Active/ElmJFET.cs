namespace Circuit.Elements.Active {
	class ElmJFET : ElmFET {
		public int mDiodeNodesA;
		public int mDiodeNodesB;
		public double mDiodeLastVdiff = 0.0;
		public double mGateCurrent;

		#region [method(Circuit)]
		protected override void DoIteration() {
			base.DoIteration();
			DiodeDoIteration(Nch * (V[IdxG] - V[IdxS]), ref mDiodeLastVdiff, mDiodeNodesA, mDiodeNodesB);
		}

		protected override double GetCurrent(int n) {
			if (n == 0) {
				return -mGateCurrent;
			}
			if (n == 1) {
				return mGateCurrent + I[0];
			}
			return -I[0];
		}

		protected override void SetCurrent(int n, double i) {
			mGateCurrent = Nch * DiodeCalculateCurrent(Nch * (V[IdxG] - V[IdxS]));
		}
		#endregion
	}
}
