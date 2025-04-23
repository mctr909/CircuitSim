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

		public override double VoltageDiff { get { return V[0]; } }

		protected override void FinishIteration() {
			mCount++; /*how many counts are in a cycle */
			mTotal += I[0] * I[0]; /* sum of squares */
			if (I[0] > mMaxI && mIncreasingI) {
				mMaxI = I[0];
				mIncreasingI = true;
				mDecreasingI = false;
			}

			if (I[0] < mMaxI && mIncreasingI) { /* change of direction I now going down - at start of waveform */
				LastMax = mMaxI; /* capture last maximum */
				/* capture time between */
				mMinI = I[0]; /* track minimum value */
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

			if (I[0] < mMinI && mDecreasingI) { /* I going down, track minimum value */
				mMinI = I[0];
				mIncreasingI = false;
				mDecreasingI = true;
			}

			if (I[0] > mMinI && mDecreasingI) { /* change of direction I now going up */
				LastMin = mMinI; /* capture last minimum */

				mMaxI = I[0];
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
			if (I[0] == 0) {
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
