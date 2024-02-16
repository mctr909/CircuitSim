namespace Circuit.Elements.Output {
	class ElmAmmeter : BaseElement {
		public const int AM_VOL = 0;
		public const int AM_RMS = 1;

		public int Meter;

		public double SelectedValue { get; private set; } = 0;
		public double RmsI { get; private set; } = 0;

		double mTotal;
		double mCount;
		double mMaxI = 0;
		double mLastMaxI;
		double mMinI = 0;
		double mLastMinI;
		int mZeroCount = 0;
		bool mIncreasingI = true;
		bool mDecreasingI = true;

		public override int TermCount { get { return 2; } }

		public override bool IsWire { get { return true; } }

		public override int VoltageSourceCount { get { return 1; } }

		public override double VoltageDiff { get { return Volts[0]; } }

		public override void Stamp() {
			CircuitElement.StampVoltageSource(Nodes[0], Nodes[1], mVoltSource, 0);
		}

		public override void IterationFinished() {
			mCount++; /*how many counts are in a cycle */
			mTotal += Current * Current; /* sum of squares */
			if (Current > mMaxI && mIncreasingI) {
				mMaxI = Current;
				mIncreasingI = true;
				mDecreasingI = false;
			}

			if (Current < mMaxI && mIncreasingI) { /* change of direction I now going down - at start of waveform */
				mLastMaxI = mMaxI; /* capture last maximum */
				/* capture time between */
				mMinI = Current; /* track minimum value */
				mIncreasingI = false;
				mDecreasingI = true;

				/* rms data */
				mTotal = mTotal / mCount;
				RmsI = Math.Sqrt(mTotal);
				if (double.IsNaN(RmsI)) {
					RmsI = 0;
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
				mLastMinI = mMinI; /* capture last minimum */

				mMaxI = Current;
				mIncreasingI = true;
				mDecreasingI = false;

				/* rms data */
				mTotal = mTotal / mCount;
				RmsI = Math.Sqrt(mTotal);
				if (double.IsNaN(RmsI)) {
					RmsI = 0;
				}
				mCount = 0;
				mTotal = 0;
			}

			/* need to zero the rms value if it stays at 0 for a while */
			if (Current == 0) {
				mZeroCount++;
				if (mZeroCount > 5) {
					mTotal = 0;
					RmsI = 0;
					mMaxI = 0;
					mMinI = 0;
				}
			} else {
				mZeroCount = 0;
			}

			switch (Meter) {
			case AM_VOL:
				SelectedValue = Current;
				break;
			case AM_RMS:
				SelectedValue = RmsI;
				break;
			}
		}

		public string getMeter() {
			switch (Meter) {
			case AM_VOL:
				return "I";
			case AM_RMS:
				return "Irms";
			}
			return "";
		}
	}
}
