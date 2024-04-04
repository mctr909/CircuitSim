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

		public ElmSweep() : base() {
			Reset();
		}

		public void SetParams() {
			if (Frequency < MinF || Frequency > MaxF) {
				Frequency = MinF;
				mFreqTime = 0;
				mFdir = 1;
			}
			if (IsLog) {
				mFadd = 0;
				mFmul = Math.Pow(MaxF / MinF, mFdir * CircuitState.DeltaTime / SweepTime);
			} else {
				mFadd = mFdir * CircuitState.DeltaTime * (MaxF - MinF) / SweepTime;
				mFmul = 1;
			}
			mSavedTimeStep = CircuitState.DeltaTime;
		}

		public override double GetVoltageDiff() {
			return NodeVolts[0];
		}

		#region [method(Analyze)]
		public override bool HasGroundConnection(int nodeIndex) { return true; }

		public override void Reset() {
			Frequency = MinF;
			mFreqTime = 0;
			mFdir = 1;
			SetParams();
		}

		public override void Stamp() {
			StampVoltageSource(0, NodeId[0], mVoltSource);
		}
		#endregion

		#region [method(Circuit)]
		public override void PrepareIteration() {
			/* has timestep been changed? */
			if (CircuitState.DeltaTime != mSavedTimeStep) {
				SetParams();
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

		public override void DoIteration() {
			UpdateVoltage(mVoltSource, mVolt);
		}
		#endregion
	}
}
