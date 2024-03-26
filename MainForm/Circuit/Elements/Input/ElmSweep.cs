namespace Circuit.Elements.Input {
	class ElmSweep : BaseElement {
		public double MaxV = 5;
		public double MaxF = 4000;
		public double MinF = 20;
		public double SweepTime = 0.1;
		public double Frequency;
		public bool IsLog = true;
		public bool BothSides = true;

		double mFadd;
		double mFmul;
		double mFreqTime;
		double mSavedTimeStep;
		double mVolt;
		int mFdir = 1;

		public override int VoltageSourceCount { get { return 1; } }

		public override int TermCount { get { return 1; } }

		public override double VoltageDiff { get { return Volts[0]; } }

		public ElmSweep() : base() {
			Reset();
		}

		public override bool HasGroundConnection(int n1) { return true; }

		public override void Reset() {
			Frequency = MinF;
			mFreqTime = 0;
			mFdir = 1;
			SetParams();
		}

		public void SetParams() {
			if (Frequency < MinF || Frequency > MaxF) {
				Frequency = MinF;
				mFreqTime = 0;
				mFdir = 1;
			}
			if (IsLog) {
				mFadd = 0;
				mFmul = Math.Pow(MaxF / MinF, mFdir * CircuitElement.delta_time / SweepTime);
			} else {
				mFadd = mFdir * CircuitElement.delta_time * (MaxF - MinF) / SweepTime;
				mFmul = 1;
			}
			mSavedTimeStep = CircuitElement.delta_time;
		}

		public override void Stamp() {
			CircuitElement.StampVoltageSource(0, Nodes[0], mVoltSource);
		}

		public override void PrepareIteration() {
			/* has timestep been changed? */
			if (CircuitElement.delta_time != mSavedTimeStep) {
				SetParams();
			}
			mVolt = Math.Sin(mFreqTime) * MaxV;
			mFreqTime += Frequency * 2 * Math.PI * CircuitElement.delta_time;
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

		public override void DoIteration() {
			var vn = CircuitElement.nodes.Length + mVoltSource;
			var row = CircuitElement.row_info[vn - 1].row;
			CircuitElement.right_side[row] += mVolt;
		}
	}
}
