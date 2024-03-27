namespace Circuit.Elements.Measure {
	class ElmAmmeter : BaseElement {
		public double Rms = 0;
		public double LastMax;
		public double LastMin;

		double mTotal;
		double mCount;
		double mMaxI = 0;
		double mMinI = 0;
		int mZeroCount = 0;
		bool mIncreasingI = true;
		bool mDecreasingI = true;

		public override int TermCount { get { return 2; } }

		public override bool IsWire { get { return true; } }

		public override int VoltageSourceCount { get { return 1; } }

		public override double VoltageDiff() {
			return Volts[0];
		}

		public override void Stamp() {
			CircuitElement.StampVoltageSource(NodeIndex[0], NodeIndex[1], mVoltSource, 0);
		}

		public override void FinishIteration() {
			mCount++; /*how many counts are in a cycle */
			mTotal += Current * Current; /* sum of squares */
			if (Current > mMaxI && mIncreasingI) {
				mMaxI = Current;
				mIncreasingI = true;
				mDecreasingI = false;
			}

			if (Current < mMaxI && mIncreasingI) { /* change of direction I now going down - at start of waveform */
				LastMax = mMaxI; /* capture last maximum */
				/* capture time between */
				mMinI = Current; /* track minimum value */
				mIncreasingI = false;
				mDecreasingI = true;

				/* rms data */
				mTotal = mTotal / mCount;
				Rms = Math.Sqrt(mTotal);
				if (double.IsNaN(Rms)) {
					Rms = 0;
				}
				mCount = 0;
				mTotal = 0;

			}

			if (Current < mMinI && mDecreasingI) { /* I going down, track minimum value */
				mMinI = Current;
				mIncreasingI = false;
				mDecreasingI = true;
			}

			if (Current > mMinI && mDecreasingI) { /* change of direction I now going up */
				LastMin = mMinI; /* capture last minimum */

				mMaxI = Current;
				mIncreasingI = true;
				mDecreasingI = false;

				/* rms data */
				mTotal = mTotal / mCount;
				Rms = Math.Sqrt(mTotal);
				if (double.IsNaN(Rms)) {
					Rms = 0;
				}
				mCount = 0;
				mTotal = 0;
			}

			/* need to zero the rms value if it stays at 0 for a while */
			if (Current == 0) {
				mZeroCount++;
				if (mZeroCount > 5) {
					mTotal = 0;
					Rms = 0;
					mMaxI = 0;
					mMinI = 0;
				}
			} else {
				mZeroCount = 0;
			}
		}
	}
}
