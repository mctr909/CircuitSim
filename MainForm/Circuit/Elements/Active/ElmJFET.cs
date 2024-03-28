namespace Circuit.Elements.Active {
	class ElmJFET : ElmFET {
		int mDiodeNodesA;
		int mDiodeNodesB;
		double mDiodeLastVdiff = 0.0;
		double mGateCurrent;

		#region [method(Analyze)]
		public override void reset() {
			base.reset();
			mDiodeLastVdiff = 0.0;
			mGateCurrent = 0.0;
		}

		public override void stamp() {
			base.stamp();
			if (Nch < 0) {
				mDiodeNodesA = node_index[IdxS];
				mDiodeNodesB = node_index[IdxG];
			} else {
				mDiodeNodesA = node_index[IdxG];
				mDiodeNodesB = node_index[IdxS];
			}
			CircuitElement.StampNonLinear(mDiodeNodesA);
			CircuitElement.StampNonLinear(mDiodeNodesB);
		}
		#endregion

		#region [method(Circuit)]
		public override void do_iteration() {
			base.do_iteration();
			DiodeDoIteration(Nch * (volts[IdxG] - volts[IdxS]), ref mDiodeLastVdiff, mDiodeNodesA, mDiodeNodesB);
		}

		public override double get_current_into_node(int n) {
			if (n == 0) {
				return -mGateCurrent;
			}
			if (n == 1) {
				return mGateCurrent + current;
			}
			return -current;
		}

		public override void set_current(int n, double c) {
			mGateCurrent = Nch * DiodeCalculateCurrent(Nch * (volts[IdxG] - volts[IdxS]));
		}
		#endregion
	}
}
