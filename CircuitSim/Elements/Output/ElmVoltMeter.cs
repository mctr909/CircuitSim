﻿using System;

namespace Circuit.Elements.Output {
    class ElmVoltMeter : BaseElement {
        public const int TP_VOL = 0;
        public const int TP_RMS = 1;
        public const int TP_MAX = 2;
        public const int TP_MIN = 3;
        public const int TP_P2P = 4;

        public int Meter;
        public E_SCALE Scale;

        public override int PostCount { get { return 2; } }

        public double RmsV { get; private set; } = 0;
        public double LastMaxV { get; private set; }
        public double LastMinV { get; private set; }

        double mTotal;
        double mCount;
        int mZeroCount = 0;
        double mMaxV = 0;
        double mMinV = 0;

        bool mIncreasingV = true;
        bool mDecreasingV = true;

        public ElmVoltMeter() : base() {
            Meter = TP_VOL;
            Scale = E_SCALE.AUTO;
        }

        public ElmVoltMeter(StringTokenizer st) : base() {
            Meter = TP_VOL;
            Scale = E_SCALE.AUTO;
            try {
                Meter = st.nextTokenInt(); /* get meter type from saved dump */
                Scale = st.nextTokenEnum<E_SCALE>();
            } catch { }
        }

        public override void CirIterationFinished() {
            mCount++; /*how many counts are in a cycle */
            double v = VoltageDiff;
            mTotal += v * v;

            /* V going up, track maximum value with */
            if (v > mMaxV && mIncreasingV) {
                mMaxV = v;
                mIncreasingV = true;
                mDecreasingV = false;
            }

            if (v < mMaxV && mIncreasingV) { /* change of direction V now going down - at start of waveform */
                LastMaxV = mMaxV; /* capture last maximum */
                                   /* capture time between */
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

        public override bool AnaGetConnection(int n1, int n2) { return false; }
    }
}
