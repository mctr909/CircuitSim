namespace Circuit.Elements.Input {
	class ElmSweep : BaseElement {
		public double MaxV = 5;
		public double MaxF = 4000;
		public double MinF = 20;
		public double SweepTime = 0.1;
		public double Frequency;
		public bool IsLog = true;
		public bool BothSides = true;

		public double mFadd;
		public double mFmul;
		public double mFreqTime;
		public double mSavedTimeStep;
		double mVolt;
		public int mFdir = 1;

		public override int TermCount { get { return 1; } }

		public override double VoltageDiff { get { return V[0]; } }

		protected override void StartIteration() {
			/* has timestep been changed? */
			if (CircuitState.DeltaTime != mSavedTimeStep) {
				//TODO:SetParams();
			}
			mVolt = Math.Sin(mFreqTime) * MaxV;
			mFreqTime += Frequency * 2 * Math.PI * CircuitState.DeltaTime;
			Frequency = Frequency * mFmul + mFadd;
			if (Frequency >= MaxF && mFdir == 1) {
				if (BothSides) {
					mFadd = -mFadd;
					mFmul = 1 / mFmul;
					mFdir = -1;
				} else {
					Frequency = MinF;
				}
			}
			if (Frequency <= MinF && mFdir == -1) {
				mFadd = -mFadd;
				mFmul = 1 / mFmul;
				mFdir = 1;
			}
		}

		protected override void DoIteration() {
			UpdateVoltageSource(VoltSource, mVolt);
		}
	}
}
