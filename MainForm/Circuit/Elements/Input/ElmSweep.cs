﻿namespace Circuit.Elements.Input {
	class ElmSweep : BaseElement {
		public double MaxV;
		public double MaxF;
		public double MinF;
		public double SweepTime;
		public bool IsLog;
		public bool BothSides;

		public double Frequency { get; private set; }

		double mFadd;
		double mFmul;
		double mFreqTime;
		double mSavedTimeStep;
		double mVolt;
		int mFdir = 1;

		public ElmSweep() : base() {
			MinF = 20;
			MaxF = 4000;
			MaxV = 5;
			SweepTime = 0.1;
			Reset();
		}

		public ElmSweep(StringTokenizer st) : base() {
			MinF = st.nextTokenDouble();
			MaxF = st.nextTokenDouble();
			MaxV = st.nextTokenDouble();
			SweepTime = st.nextTokenDouble();
			Reset();
		}

		public override int VoltageSourceCount { get { return 1; } }

		public override int TermCount { get { return 1; } }

		public override double VoltageDiff { get { return Volts[0]; } }

		public override void Reset() {
			Frequency = MinF;
			mFreqTime = 0;
			mFdir = 1;
			setParams();
		}

		public void setParams() {
			if (Frequency < MinF || Frequency > MaxF) {
				Frequency = MinF;
				mFreqTime = 0;
				mFdir = 1;
			}
			if (IsLog) {
				mFadd = 0;
				mFmul = Math.Pow(MaxF / MinF, mFdir * CircuitElement.TimeStep / SweepTime);
			} else {
				mFadd = mFdir * CircuitElement.TimeStep * (MaxF - MinF) / SweepTime;
				mFmul = 1;
			}
			mSavedTimeStep = CircuitElement.TimeStep;
		}

		public override bool HasGroundConnection(int n1) { return true; }

		public override void Stamp() {
			CircuitElement.StampVoltageSource(0, Nodes[0], mVoltSource);
		}

		public override void PrepareIteration() {
			/* has timestep been changed? */
			if (CircuitElement.TimeStep != mSavedTimeStep) {
				setParams();
			}
			mVolt = Math.Sin(mFreqTime) * MaxV;
			mFreqTime += Frequency * 2 * Math.PI * CircuitElement.TimeStep;
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
			var vn = CircuitElement.Nodes.Count + mVoltSource;
			var row = CircuitElement.RowInfo[vn - 1].MapRow;
			CircuitElement.RightSide[row] += mVolt;
		}
	}
}
