namespace Circuit.Elements.Input {
	class ElmVoltage : BaseElement {
		public enum WAVEFORM {
			DC,
			SIN,
			SQUARE,
			TRIANGLE,
			SAWTOOTH,
			PULSE_MONOPOLE,
			PULSE_DIPOLE,
			PWM_MONOPOLE,
			PWM_DIPOLE,
			PWM_POSITIVE,
			PWM_NEGATIVE,
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

		public ElmVoltage(WAVEFORM wf) {
			WaveForm = wf;
			MaxVoltage = 5;
			Frequency = 100;
			DutyCycle = 0.5;
		}

		public ElmVoltage(StringTokenizer st) {
			WaveForm = st.nextTokenEnum(WAVEFORM.DC);
			Frequency = st.nextTokenDouble(100);
			MaxVoltage = st.nextTokenDouble(5);
			Bias = st.nextTokenDouble();
			Phase = st.nextTokenDouble() * Math.PI / 180;
			PhaseOffset = st.nextTokenDouble() * Math.PI / 180;
			DutyCycle = st.nextTokenDouble(0.5);
		}

		public override int TermCount { get { return 2; } }

		public override int VoltageSourceCount { get { return 1; } }

		public override double VoltageDiff { get { return Volts[1] - Volts[0]; } }

		public override void Reset() { }

		public override void Stamp() {
			int n0 = Nodes[0] - 1;
			int n1 = Nodes[1] - 1;
			int vn = Circuit.Nodes.Count + mVoltSource - 1;
			if (n0 < 0 || n1 < 0 || vn < 0) {
				return;
			}
			Circuit.Matrix[vn, n0] -= 1;
			Circuit.Matrix[vn, n1] += 1;
			Circuit.Matrix[n0, vn] += 1;
			Circuit.Matrix[n1, vn] -= 1;
			if (WaveForm == WAVEFORM.DC) {
				Circuit.RightSide[vn] += GetVoltage();
			} else {
				Circuit.RowInfo[vn].RightChanges = true;
			}
		}

		public override void DoIteration() {
			if (WaveForm != WAVEFORM.DC) {
				var vn = Circuit.Nodes.Count + mVoltSource;
				var row = Circuit.RowInfo[vn - 1].MapRow;
				Circuit.RightSide[row] += GetVoltage();
			}
		}

		public override void IterationFinished() {
			if (WaveForm == WAVEFORM.NOISE) {
				NoiseValue = (Circuit.Random.NextDouble() * 2 - 1) * MaxVoltage + Bias;
			}
		}

		public double GetVoltage() {
			double t = Circuit.Time;
			double wt = 2 * Math.PI * Frequency * t;
			double ph = Phase + PhaseOffset;
			double duty = 2 * Math.PI * DutyCycle;
			double cycle = (wt + ph) % (2 * Math.PI);

			switch (WaveForm) {
			case WAVEFORM.DC:
				return MaxVoltage + Bias;
			case WAVEFORM.SIN:
				return Math.Sin(wt + ph) * MaxVoltage + Bias;
			case WAVEFORM.SQUARE:
				return Bias + (cycle > duty ? -MaxVoltage : MaxVoltage);
			case WAVEFORM.TRIANGLE:
				return Bias + triangleFunc(cycle) * MaxVoltage;
			case WAVEFORM.SAWTOOTH:
				return Bias + cycle * (MaxVoltage / Math.PI) - MaxVoltage;
			case WAVEFORM.PULSE_MONOPOLE:
				return cycle < duty ? (MaxVoltage + Bias) : Bias;
			case WAVEFORM.PULSE_DIPOLE:
				if (cycle < Math.PI) {
					return 2 * cycle < duty ? (Bias + MaxVoltage) : Bias;
				} else {
					return 2 * (cycle - Math.PI) < duty ? (Bias - MaxVoltage) : Bias;
				}
			case WAVEFORM.PWM_MONOPOLE: {
				var maxwt = 2 * Math.PI * t / (64 * Circuit.TimeStep);
				var cr = triangleFunc(maxwt % (2 * Math.PI)) * 0.5 + 0.5;
				var sg = DutyCycle * Math.Sin(wt + ph) * 0.5 + 0.5;
				return Bias + (cr < sg ? MaxVoltage : 0);
			}
			case WAVEFORM.PWM_DIPOLE: {
				var maxwt = 2 * Math.PI * t / (64 * Circuit.TimeStep);
				var cr = triangleFunc(maxwt % (2 * Math.PI)) * 0.5 + 0.5;
				var sg = DutyCycle * Math.Sin(wt + ph);
				if (0.0 <= sg) {
					return Bias + (cr < sg ? MaxVoltage : 0);
				} else {
					return Bias - (sg < -cr ? MaxVoltage : 0);
				}
			}
			case WAVEFORM.PWM_POSITIVE: {
				var maxwt = 2 * Math.PI * t / (64 * Circuit.TimeStep);
				var cr = triangleFunc(maxwt % (2 * Math.PI)) * 0.5 + 0.5;
				var sg = DutyCycle * Math.Sin(wt + ph);
				if (0.0 < sg) {
					return Bias + (cr < sg ? MaxVoltage : 0);
				} else {
					return Bias;
				}
			}
			case WAVEFORM.PWM_NEGATIVE: {
				var maxwt = 2 * Math.PI * t / (64 * Circuit.TimeStep);
				var cr = triangleFunc(maxwt % (2 * Math.PI)) * 0.5 + 0.5;
				var sg = DutyCycle * Math.Sin(wt + ph);
				if (0.0 > sg) {
					return Bias + (cr < -sg ? MaxVoltage : 0);
				} else {
					return Bias;
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
