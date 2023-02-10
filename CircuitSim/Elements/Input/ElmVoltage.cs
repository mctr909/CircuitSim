using System;

namespace Circuit.Elements.Input {
    class ElmVoltage : BaseElement {
        public enum WAVEFORM {
            DC,
            AC,
            SQUARE,
            TRIANGLE,
            SAWTOOTH,
            PULSE,
            PULSE_BOTH,
            PWM,
            PWM_BOTH,
            NOISE
        }

        public double Frequency;
        public double MaxVoltage;
        public double Bias;
        public double Phase;
        public double PhaseOffset;
        public double DutyCycle;
        public double NoiseValue;
        public WAVEFORM WaveForm;
        public int LinkBias;
        public int LinkFrequency;
        public int LinkPhaseOffset;

        public ElmVoltage(WAVEFORM wf) {
            WaveForm = wf;
            MaxVoltage = 5;
            Frequency = 100;
            DutyCycle = .5;
        }

        public ElmVoltage(StringTokenizer st) {
            MaxVoltage = 5;
            Frequency = 100;
            WaveForm = WAVEFORM.DC;
            DutyCycle = .5;
            try {
                WaveForm = st.nextTokenEnum<WAVEFORM>();
                Frequency = st.nextTokenDouble();
                MaxVoltage = st.nextTokenDouble();
                Bias = st.nextTokenDouble();
                Phase = st.nextTokenDouble() * Math.PI / 180;
                PhaseOffset = st.nextTokenDouble() * Math.PI / 180;
                DutyCycle = st.nextTokenDouble();
                LinkBias = st.nextTokenInt();
                LinkFrequency = st.nextTokenInt();
                LinkPhaseOffset = st.nextTokenInt();
            } catch { }
        }

        public override int PostCount { get { return 2; } }

        public override double VoltageDiff { get { return Volts[1] - Volts[0]; } }

        public override double Power { get { return -VoltageDiff * Current; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override void Reset() { }

        public override void AnaStamp() {
            if (WaveForm == WAVEFORM.DC) {
                Circuit.StampVoltageSource(Nodes[0], Nodes[1], mVoltSource, GetVoltage());
            } else {
                Circuit.StampVoltageSource(Nodes[0], Nodes[1], mVoltSource);
            }
        }

        public override void CirDoIteration() {
            if (WaveForm != WAVEFORM.DC) {
                var vn = Circuit.NodeList.Count + mVoltSource;
                var row = Circuit.RowInfo[vn - 1].MapRow;
                Circuit.RightSide[row] += GetVoltage();
            }
        }

        public override void CirIterationFinished() {
            if (WaveForm == WAVEFORM.NOISE) {
                NoiseValue = (CirSimForm.Random.NextDouble() * 2 - 1) * MaxVoltage + Bias;
            }
        }

        public double GetVoltage() {
            double t = CirSimForm.Time;
            double wt = 2 * Math.PI * Frequency * t + Phase + PhaseOffset;
            double duty = 2 * Math.PI * DutyCycle;
            double cycle = wt % (2 * Math.PI);

            switch (WaveForm) {
            case WAVEFORM.DC:
                return MaxVoltage + Bias;
            case WAVEFORM.AC:
                return Math.Sin(wt) * MaxVoltage + Bias;
            case WAVEFORM.SQUARE:
                return Bias + (cycle > duty ? -MaxVoltage : MaxVoltage);
            case WAVEFORM.TRIANGLE:
                return Bias + triangleFunc(cycle) * MaxVoltage;
            case WAVEFORM.SAWTOOTH:
                return Bias + cycle * (MaxVoltage / Math.PI) - MaxVoltage;
            case WAVEFORM.PULSE:
                return cycle < duty ? (MaxVoltage + Bias) : Bias;
            case WAVEFORM.PULSE_BOTH:
                if (cycle < Math.PI) {
                    return 2 * cycle < duty ? (Bias + MaxVoltage) : Bias;
                } else {
                    return 2 * (cycle - Math.PI) < duty ? (Bias - MaxVoltage) : Bias;
                }
            case WAVEFORM.PWM: {
                var maxwt = 2 * Math.PI * t / (32 * ControlPanel.TimeStep);
                var cr = 0.5 - 0.5 * triangleFunc(maxwt % (2 * Math.PI));
                var sg = DutyCycle * Math.Sin(wt);
                if (0.0 <= sg) {
                    return Bias + (cr < sg ? MaxVoltage : 0);
                } else {
                    return Bias;
                }
            }
            case WAVEFORM.PWM_BOTH: {
                var maxwt = 2 * Math.PI * t / (32 * ControlPanel.TimeStep);
                var cr = 0.5 - 0.5 * triangleFunc(maxwt % (2 * Math.PI));
                var sg = DutyCycle * Math.Sin(wt);
                if (0.0 <= sg) {
                    return Bias + (cr < sg ? MaxVoltage : 0);
                } else {
                    return Bias - (sg < -cr ? MaxVoltage : 0);
                }
            }
            case WAVEFORM.NOISE:
                return NoiseValue;
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
