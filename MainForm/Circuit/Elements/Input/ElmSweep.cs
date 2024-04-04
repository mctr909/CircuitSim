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
			reset();
		}

		public void SetParams() {
			if (Frequency < MinF || Frequency > MaxF) {
				Frequency = MinF;
				mFreqTime = 0;
				mFdir = 1;
			}
			if (IsLog) {
				mFadd = 0;
				mFmul = Math.Pow(MaxF / MinF, mFdir * CircuitElement.DeltaTime / SweepTime);
			} else {
				mFadd = mFdir * CircuitElement.DeltaTime * (MaxF - MinF) / SweepTime;
				mFmul = 1;
			}
			mSavedTimeStep = CircuitElement.DeltaTime;
		}

		public override double voltage_diff() {
			return volts[0];
		}

		#region [method(Analyze)]
		public override bool has_ground_connection(int n1) { return true; }

		public override void reset() {
			Frequency = MinF;
			mFreqTime = 0;
			mFdir = 1;
			SetParams();
		}

		public override void stamp() {
			CircuitElement.StampVoltageSource(0, node_index[0], m_volt_source);
		}
		#endregion

		#region [method(Circuit)]
		public override void prepare_iteration() {
			/* has timestep been changed? */
			if (CircuitElement.DeltaTime != mSavedTimeStep) {
				SetParams();
			}
			mVolt = Math.Sin(mFreqTime) * MaxV;
			mFreqTime += Frequency * 2 * Math.PI * CircuitElement.DeltaTime;
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

		public override void do_iteration() {
			var vn = CircuitElement.NodeCount + m_volt_source;
			var row = CircuitElement.NodeInfo[vn - 1].ROW;
			CircuitElement.RightSide[row] += mVolt;
		}
		#endregion
	}
}
