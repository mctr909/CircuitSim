namespace Circuit.Elements.Active {
	class ElmJFET : ElmFET {
		Diode mDiode;
		double mGateCurrent;

		public ElmJFET() : base() {
			mDiode = new Diode();
			mDiode.SetupForDefaultModel();
		}

		public override void Reset() {
			base.Reset();
			mDiode.Reset();
		}

		public override void Stamp() {
			base.Stamp();
			if (Nch < 0) {
				mDiode.Stamp(Nodes[IdxS], Nodes[IdxG]);
			} else {
				mDiode.Stamp(Nodes[IdxG], Nodes[IdxS]);
			}
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
			mDiode.DoIteration(Nch * (Volts[IdxG] - Volts[IdxS]));
		}

		public override void SetCurrent(int n, double c) {
			mGateCurrent = Nch * mDiode.CalculateCurrent(Nch * (Volts[IdxG] - Volts[IdxS]));
		}
	}
}
