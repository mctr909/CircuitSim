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

		public override double voltage_diff() {
			return volts[0];
		}

		public override void stamp() {
			CircuitElement.StampVoltageSource(node_index[0], node_index[1], m_volt_source, 0);
		}

		public override void finish_iteration() {
			mCount++; /*how many counts are in a cycle */
			mTotal += current * current; /* sum of squares */
			if (current > mMaxI && mIncreasingI) {
				mMaxI = current;
				mIncreasingI = true;
				mDecreasingI = false;
			}

			if (current < mMaxI && mIncreasingI) { /* change of direction I now going down - at start of waveform */
				LastMax = mMaxI; /* capture last maximum */
				/* capture time between */
				mMinI = current; /* track minimum value */
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

			if (current < mMinI && mDecreasingI) { /* I going down, track minimum value */
				mMinI = current;
				mIncreasingI = false;
				mDecreasingI = true;
			}

			if (current > mMinI && mDecreasingI) { /* change of direction I now going up */
				LastMin = mMinI; /* capture last minimum */

				mMaxI = current;
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
			if (current == 0) {
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
