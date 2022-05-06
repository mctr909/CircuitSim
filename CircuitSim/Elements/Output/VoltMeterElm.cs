using System;

namespace Circuit.Elements.Output {
    class VoltMeterElm : BaseElement {
        const int FLAG_SHOWVOLTAGE = 1;

        public const int TP_VOL = 0;
        public const int TP_RMS = 1;
        public const int TP_MAX = 2;
        public const int TP_MIN = 3;
        public const int TP_P2P = 4;
        public const int TP_BIN = 5;
        public const int TP_FRQ = 6;
        public const int TP_PER = 7;
        public const int TP_PWI = 8;
        public const int TP_DUT = 9; /* mark to space ratio */

        public int Meter;
        public E_SCALE Scale;

        public double RmsV { get; private set; } = 0;
        public double BinaryLevel { get; private set; } = 0; /*0 or 1 - double because we only pass doubles back to the web page */
        public double LastMaxV { get; private set; }
        public double LastMinV { get; private set; }
        public double Frequency { get; private set; } = 0;
        public double Period { get; private set; } = 0;
        public double PulseWidth { get; private set; } = 0;
        public double DutyCycle { get; private set; } = 0;
        public double SelectedValue { get; private set; } = 0;

        double mTotal;
        double mCount;
        int mZeroCount = 0;
        double mMaxV = 0;
        double mMinV = 0;

        bool mIncreasingV = true;
        bool mDecreasingV = true;

        long mPeriodStart; /* time between consecutive max values */
        long mPeriodLength;
        long mPulseStart;

        public VoltMeterElm() : base() {
            Meter = TP_VOL;
            Scale = E_SCALE.AUTO;
        }

        public VoltMeterElm(StringTokenizer st) : base() {
            Meter = TP_VOL;
            Scale = E_SCALE.AUTO;
            try {
                Meter = st.nextTokenInt(); /* get meter type from saved dump */
                Scale = st.nextTokenEnum<E_SCALE>();
            } catch { }
        }

        public override int PostCount { get { return 2; } }

        public override void CirStepFinished() {
            mCount++; /*how many counts are in a cycle */
            double v = VoltageDiff;
            mTotal += v * v;

            if (v < 2.5) {
                BinaryLevel = 0;
            } else {
                BinaryLevel = 1;
            }

            /* V going up, track maximum value with */
            if (v > mMaxV && mIncreasingV) {
                mMaxV = v;
                mIncreasingV = true;
                mDecreasingV = false;
            }

            if (v < mMaxV && mIncreasingV) { /* change of direction V now going down - at start of waveform */
                LastMaxV = mMaxV; /* capture last maximum */
                                   /* capture time between */
                var now = DateTime.Now.ToFileTimeUtc();
                mPeriodLength = now - mPeriodStart;
                mPeriodStart = now;
                Period = mPeriodLength;
                PulseWidth = now - mPulseStart;
                DutyCycle = PulseWidth / mPeriodLength;
                mMinV = v; /* track minimum value with V */
                mIncreasingV = false;
                mDecreasingV = true;

                /* rms data */
                mTotal = mTotal / mCount;
                RmsV = Math.Sqrt(mTotal);
                if (double.IsNaN(RmsV)) {
                    RmsV = 0;
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
                LastMinV = mMinV; /* capture last minimum */
                mPulseStart = DateTime.Now.ToFileTimeUtc();
                mMaxV = v;
                mIncreasingV = true;
                mDecreasingV = false;

                /* rms data */
                mTotal = mTotal / mCount;
                RmsV = Math.Sqrt(mTotal);
                if (double.IsNaN(RmsV)) {
                    RmsV = 0;
                }
                mCount = 0;
                mTotal = 0;
            }

            /* need to zero the rms value if it stays at 0 for a while */
            if (v == 0) {
                mZeroCount++;
                if (mZeroCount > 5) {
                    mTotal = 0;
                    RmsV = 0;
                    mMaxV = 0;
                    mMinV = 0;
                }
            } else {
                mZeroCount = 0;
            }
        }

        public override bool GetConnection(int n1, int n2) { return false; }

        public string getMeter() {
            switch (Meter) {
            case TP_VOL:
                return "V";
            case TP_RMS:
                return "V(rms)";
            case TP_MAX:
                return "Vmax";
            case TP_MIN:
                return "Vmin";
            case TP_P2P:
                return "Peak to peak";
            case TP_BIN:
                return "Binary";
            case TP_FRQ:
                return "Frequency";
            case TP_PER:
                return "Period";
            case TP_PWI:
                return "Pulse width";
            case TP_DUT:
                return "Duty cycle";
            }
            return "";
        }
    }
}
