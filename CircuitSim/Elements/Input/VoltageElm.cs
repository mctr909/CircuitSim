using System;

namespace Circuit.Elements.Input {
    class VoltageElm : BaseElement {
        public enum WAVEFORM {
            DC,
            AC,
            SQUARE,
            TRIANGLE,
            SAWTOOTH,
            PULSE,
            PULSE_BOTH,
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

        public VoltageElm(WAVEFORM wf) {
            waveform = wf;
            mMaxVoltage = 5;
            mFrequency = 40;
            mDutyCycle = .5;
            Reset();
        }

        public VoltageElm(StringTokenizer st) {
            mMaxVoltage = 5;
            mFrequency = 40;
            waveform = WAVEFORM.DC;
            mDutyCycle = .5;

            try {
                waveform = st.nextTokenEnum<WAVEFORM>();
                mFrequency = st.nextTokenDouble();
                mMaxVoltage = st.nextTokenDouble();
                mBias = st.nextTokenDouble();
                mPhaseShift = st.nextTokenDouble() * Math.PI / 180;
                mDutyCycle = st.nextTokenDouble();
            } catch { }

            Reset();
        }

        public override int PostCount { get { return 2; } }

        public override double VoltageDiff { get { return Volts[1] - Volts[0]; } }

        public override double Power { get { return -VoltageDiff * mCurrent; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override void Reset() {
            CurCount = 0;
        }

        public override void AnaStamp() {
            if (waveform == WAVEFORM.DC) {
                Circuit.StampVoltageSource(Nodes[0], Nodes[1], mVoltSource, getVoltage());
            } else {
                Circuit.StampVoltageSource(Nodes[0], Nodes[1], mVoltSource);
            }
        }

        public override void CirDoStep() {
            if (waveform != WAVEFORM.DC) {
                Circuit.UpdateVoltageSource(Nodes[0], Nodes[1], mVoltSource, getVoltage());
            }
        }

        public override void CirStepFinished() {
            if (waveform == WAVEFORM.NOISE) {
                mNoiseValue = (CirSimForm.Random.NextDouble() * 2 - 1) * mMaxVoltage + mBias;
            }
        }

        public virtual double getVoltage() {
            if (waveform != WAVEFORM.DC && CirSimForm.Sim.DcAnalysisFlag) {
                return mBias;
            }

            double t = CirSimForm.Sim.Time;
            double wt = 2 * Math.PI * mFrequency * t + mPhaseShift;
            double duty = 2 * Math.PI * mDutyCycle;
            double cycle = wt % (2 * Math.PI);

            switch (waveform) {
            case WAVEFORM.DC:
                return mMaxVoltage + mBias;
            case WAVEFORM.AC:
                return Math.Sin(wt) * mMaxVoltage + mBias;
            case WAVEFORM.SQUARE:
                return mBias + (cycle > duty ? -mMaxVoltage : mMaxVoltage);
            case WAVEFORM.TRIANGLE:
                return mBias + triangleFunc(cycle) * mMaxVoltage;
            case WAVEFORM.SAWTOOTH:
                return mBias + cycle * (mMaxVoltage / Math.PI) - mMaxVoltage;
            case WAVEFORM.PULSE:
                return cycle < duty ? (mMaxVoltage + mBias) : mBias;
            case WAVEFORM.PULSE_BOTH:
                if (cycle < Math.PI) {
                    return 2 * cycle < duty ? (mBias + mMaxVoltage) : mBias;
                } else {
                    return 2 * (cycle - Math.PI) < duty ? (mBias - mMaxVoltage) : mBias;
                }
            case WAVEFORM.PWM_BOTH: {
                var maxwt = 2 * Math.PI * t / (32 * ControlPanel.TimeStep);
                var cr = 0.5 - 0.5 * triangleFunc(maxwt % (2 * Math.PI));
                var sg = mDutyCycle * Math.Sin(wt);
                if (0.0 <= sg) {
                    return mBias + (cr < sg ? mMaxVoltage : 0);
                } else {
                    return mBias - (sg < -cr ? mMaxVoltage : 0);
                }
            }
            case WAVEFORM.PWM_POSITIVE: {
                var maxwt = 2 * Math.PI * t / (32 * ControlPanel.TimeStep);
                var cr = 0.5 - 0.5 * triangleFunc(maxwt % (2 * Math.PI));
                var sg = mDutyCycle * Math.Sin(wt);
                if (0.0 <= sg) {
                    return mBias + (cr < sg ? mMaxVoltage : 0);
                } else {
                    return mBias;
                }
            }
            case WAVEFORM.PWM_NEGATIVE: {
                var maxwt = 2 * Math.PI * t / (32 * ControlPanel.TimeStep);
                var cr = 0.5 - 0.5 * triangleFunc(maxwt % (2 * Math.PI));
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
