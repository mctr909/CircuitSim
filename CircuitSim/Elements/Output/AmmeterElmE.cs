using System;

namespace Circuit.Elements.Output {
    class AmmeterElmE : BaseElement {
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

        public AmmeterElmE() : base() {
            Scale = E_SCALE.AUTO;
        }

        public AmmeterElmE(StringTokenizer st) : base() {
            Meter = st.nextTokenInt();
            try {
                Scale = st.nextTokenEnum<E_SCALE>();
            } catch {
                Scale = E_SCALE.AUTO;
            }
        }

        public override bool CirIsWire { get { return true; } }

        public override double CirVoltageDiff { get { return CirVolts[0]; } }

        public override double CirPower { get { return 0; } }

        public override int CirVoltageSourceCount { get { return 1; } }

        public override void CirStepFinished() {
            mCount++; /*how many counts are in a cycle */
            mTotal += mCirCurrent * mCirCurrent; /* sum of squares */
            if (mCirCurrent > mMaxI && mIncreasingI) {
                mMaxI = mCirCurrent;
                mIncreasingI = true;
                mDecreasingI = false;
            }

            if (mCirCurrent < mMaxI && mIncreasingI) { /* change of direction I now going down - at start of waveform */
                mLastMaxI = mMaxI; /* capture last maximum */
                                 /* capture time between */
                mMinI = mCirCurrent; /* track minimum value */
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

            if (mCirCurrent < mMinI && mDecreasingI) { /* I going down, track minimum value */
                mMinI = mCirCurrent;
                mIncreasingI = false;
                mDecreasingI = true;
            }

            if (mCirCurrent > mMinI && mDecreasingI) { /* change of direction I now going up */
                mLastMinI = mMinI; /* capture last minimum */

                mMaxI = mCirCurrent;
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
            if (mCirCurrent == 0) {
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
                SelectedValue = mCirCurrent;
                break;
            case AM_RMS:
                SelectedValue = RmsI;
                break;
            }
        }

        public override void CirStamp() {
            mCir.StampVoltageSource(CirNodes[0], CirNodes[1], mCirVoltSource, 0);
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
