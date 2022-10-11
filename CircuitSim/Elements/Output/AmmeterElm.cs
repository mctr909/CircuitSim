using System;

namespace Circuit.Elements.Output {
    class AmmeterElm : BaseElement {
        public const int AM_VOL = 0;
        public const int AM_RMS = 1;

        public int Meter;
        public E_SCALE Scale;

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

        public AmmeterElm() : base() {
            Scale = E_SCALE.AUTO;
        }

        public AmmeterElm(StringTokenizer st) : base() {
            Meter = st.nextTokenInt();
            try {
                Scale = st.nextTokenEnum<E_SCALE>();
            } catch {
                Scale = E_SCALE.AUTO;
            }
        }

        public override int PostCount { get { return 2; } }

        public override bool IsWire { get { return true; } }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override double Power { get { return 0; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override void CirIterationFinished() {
            mCount++; /*how many counts are in a cycle */
            mTotal += mCurrent * mCurrent; /* sum of squares */
            if (mCurrent > mMaxI && mIncreasingI) {
                mMaxI = mCurrent;
                mIncreasingI = true;
                mDecreasingI = false;
            }

            if (mCurrent < mMaxI && mIncreasingI) { /* change of direction I now going down - at start of waveform */
                mLastMaxI = mMaxI; /* capture last maximum */
                                 /* capture time between */
                mMinI = mCurrent; /* track minimum value */
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

            if (mCurrent < mMinI && mDecreasingI) { /* I going down, track minimum value */
                mMinI = mCurrent;
                mIncreasingI = false;
                mDecreasingI = true;
            }

            if (mCurrent > mMinI && mDecreasingI) { /* change of direction I now going up */
                mLastMinI = mMinI; /* capture last minimum */

                mMaxI = mCurrent;
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
            if (mCurrent == 0) {
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
                SelectedValue = mCurrent;
                break;
            case AM_RMS:
                SelectedValue = RmsI;
                break;
            }
        }

        public override void AnaStamp() {
            Circuit.StampVoltageSource(Nodes[0], Nodes[1], mVoltSource, 0);
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
