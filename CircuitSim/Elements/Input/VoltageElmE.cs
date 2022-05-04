using System;

namespace Circuit.Elements.Input {
    class VoltageElmE : BaseElement {
        public enum WAVEFORM {
            DC,
            AC,
            SQUARE,
            TRIANGLE,
            SAWTOOTH,
            PULSE,
            PWM_BOTH,
            PWM_POSITIVE,
            PWM_NEGATIVE,
            NOISE
        }

        public double mFrequency;
        public double mMaxVoltage;
        public double mBias;
        public double mPhaseShift;
        public double mDutyCycle;
        public double mNoiseValue;
        public WAVEFORM waveform;      

        public VoltageElmE(WAVEFORM wf) {
            waveform = wf;
            mMaxVoltage = 5;
            mFrequency = 40;
            mDutyCycle = .5;
            CirReset();
        }

        public VoltageElmE(StringTokenizer st) {
            mMaxVoltage = 5;
            mFrequency = 40;
            waveform = WAVEFORM.DC;
            mDutyCycle = .5;

            try {
                waveform = st.nextTokenEnum<WAVEFORM>();
                mFrequency = st.nextTokenDouble();
                mMaxVoltage = st.nextTokenDouble();
                mBias = st.nextTokenDouble();
                mPhaseShift = st.nextTokenDouble();
                mDutyCycle = st.nextTokenDouble();
            } catch { }

            CirReset();
        }

        public override double CirVoltageDiff { get { return CirVolts[1] - CirVolts[0]; } }

        public override double CirPower { get { return -CirVoltageDiff * mCirCurrent; } }

        public override int CirVoltageSourceCount { get { return 1; } }

        public override void CirReset() {
            mCirCurCount = 0;
        }

        public override void CirStamp() {
            if (waveform == WAVEFORM.DC) {
                mCir.StampVoltageSource(CirNodes[0], CirNodes[1], mCirVoltSource, getVoltage());
            } else {
                mCir.StampVoltageSource(CirNodes[0], CirNodes[1], mCirVoltSource);
            }
        }

        public override void CirDoStep() {
            if (waveform != WAVEFORM.DC) {
                mCir.UpdateVoltageSource(CirNodes[0], CirNodes[1], mCirVoltSource, getVoltage());
            }
        }

        public override void CirStepFinished() {
            if (waveform == WAVEFORM.NOISE) {
                mNoiseValue = (CirSim.Random.NextDouble() * 2 - 1) * mMaxVoltage + mBias;
            }
        }

        public virtual double getVoltage() {
            if (waveform != WAVEFORM.DC && CirSim.Sim.DcAnalysisFlag) {
                return mBias;
            }

            double t = 2 * Math.PI * CirSim.Sim.Time;
            double wt = t * mFrequency + mPhaseShift;

            switch (waveform) {
            case WAVEFORM.DC:
                return mMaxVoltage + mBias;
            case WAVEFORM.AC:
                return Math.Sin(wt) * mMaxVoltage + mBias;
            case WAVEFORM.SQUARE:
                return mBias + ((wt % (2 * Math.PI) > ((2 * Math.PI) * mDutyCycle)) ? -mMaxVoltage : mMaxVoltage);
            case WAVEFORM.TRIANGLE:
                return mBias + triangleFunc(wt % (2 * Math.PI)) * mMaxVoltage;
            case WAVEFORM.SAWTOOTH:
                return mBias + (wt % (2 * Math.PI)) * (mMaxVoltage / Math.PI) - mMaxVoltage;
            case WAVEFORM.PULSE:
                return ((wt % (2 * Math.PI)) < ((2 * Math.PI) * mDutyCycle)) ? mMaxVoltage + mBias : mBias;
            case WAVEFORM.PWM_BOTH: {
                var maxfreq = 1 / (32 * ControlPanel.TimeStep);
                var cr = 0.5 - 0.5 * triangleFunc(t * maxfreq % (2 * Math.PI));
                var sg = mDutyCycle * Math.Sin(wt);
                if (0.0 <= sg) {
                    return mBias + (cr < sg ? mMaxVoltage : 0);
                } else {
                    return mBias - (sg < -cr ? mMaxVoltage : 0);
                }
            }
            case WAVEFORM.PWM_POSITIVE: {
                var maxfreq = 1 / (32 * ControlPanel.TimeStep);
                var cr = 0.5 - 0.5 * triangleFunc(t * maxfreq % (2 * Math.PI));
                var sg = mDutyCycle * Math.Sin(wt);
                if (0.0 <= sg) {
                    return mBias + (cr < sg ? mMaxVoltage : 0);
                } else {
                    return mBias;
                }
            }
            case WAVEFORM.PWM_NEGATIVE: {
                var maxfreq = 1 / (32 * ControlPanel.TimeStep);
                var cr = 0.5 - 0.5 * triangleFunc(t * maxfreq % (2 * Math.PI));
                var sg = mDutyCycle * Math.Sin(wt);
                if (0.0 <= sg) {
                    return mBias;
                } else {
                    return mBias + (sg < -cr ? mMaxVoltage : 0);
                }
            }
            case WAVEFORM.NOISE:
                return mNoiseValue;
            default: return 0;
            }
        }

        double triangleFunc(double x) {
            if (x < Math.PI) {
                return x * (2 / Math.PI) - 1;
            }
            return 1 - (x - Math.PI) * (2 / Math.PI);
        }
    }
}
