namespace Circuit.Elements.Measure {
	class ElmVoltMeter : BaseElement {
		public double Rms = 0;
		public double LastMax;
		public double LastMin;

		double mTotal;
		double mCount;
		double mMaxV = 0;
		double mMinV = 0;
		int mZeroCount = 0;
		bool mIncreasingV = true;
		bool mDecreasingV = true;

		protected override void FinishIteration() {
			mCount++; /*how many counts are in a cycle */
			var v = VoltageDiff;
			mTotal += v * v;

			/* V going up, track maximum value with */
			if (v > mMaxV && mIncreasingV) {
				mMaxV = v;
				mIncreasingV = true;
				mDecreasingV = false;
			}

			if (v < mMaxV && mIncreasingV) { /* change of direction V now going down - at start of waveform */
				LastMax = mMaxV; /* capture last maximum */
				/* capture time between */
				mMinV = v; /* track minimum value with V */
				mIncreasingV = false;
				mDecreasingV = true;

				/* rms data */
				mTotal = mTotal / mCount;
				Rms = Math.Sqrt(mTotal);
				if (double.IsNaN(Rms)) {
					Rms = 0;
				}
				mCount = 0;
				mTotal = 0;
			}

			if (v < mMinV && mDecreasingV) { /* V going down, track minimum value with V */
				mMinV = v;
				mIncreasingV = false;
				mDecreasingV = true;
			}

			if (v > mMinV && mDecreasingV) { /* change of direction V now going up */
				LastMin = mMinV; /* capture last minimum */
				mMaxV = v;
				mIncreasingV = true;
				mDecreasingV = false;

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
			if (v == 0) {
				mZeroCount++;
				if (mZeroCount > 5) {
					mTotal = 0;
					Rms = 0;
					mMaxV = 0;
					mMinV = 0;
				}
			} else {
				mZeroCount = 0;
			}
		}
	}
}
