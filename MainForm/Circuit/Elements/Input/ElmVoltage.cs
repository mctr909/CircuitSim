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

		public double Frequency = 100;
		public double MaxVoltage = 5;
		public double Bias;
		public double Phase;
		public double PhaseOffset;
		public double DutyCycle = 0.5;
		public double NoiseValue;
		public WAVEFORM WaveForm;

		public override int TermCount { get { return 2; } }

		public override int VoltageSourceCount { get { return 1; } }

		public override double GetVoltageDiff() {
			return NodeVolts[1] - NodeVolts[0];
		}

		public double GetVoltage() {
			double t = CircuitState.Time;
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
				var maxwt = 2 * Math.PI * t / (64 * CircuitState.DeltaTime);
				var cr = triangleFunc(maxwt % (2 * Math.PI)) * 0.5 + 0.5;
				var sg = DutyCycle * Math.Sin(wt + ph) * 0.5 + 0.5;
				return Bias + (cr < sg ? MaxVoltage : 0);
			}
			case WAVEFORM.PWM_DIPOLE: {
				var maxwt = 2 * Math.PI * t / (64 * CircuitState.DeltaTime);
				var cr = triangleFunc(maxwt % (2 * Math.PI)) * 0.5 + 0.5;
				var sg = DutyCycle * Math.Sin(wt + ph);
				if (0.0 <= sg) {
					return Bias + (cr < sg ? MaxVoltage : 0);
				} else {
					return Bias - (sg < -cr ? MaxVoltage : 0);
				}
			}
			case WAVEFORM.PWM_POSITIVE: {
				var maxwt = 2 * Math.PI * t / (64 * CircuitState.DeltaTime);
				var cr = triangleFunc(maxwt % (2 * Math.PI)) * 0.5 + 0.5;
				var sg = DutyCycle * Math.Sin(wt + ph);
				if (0.0 < sg) {
					return Bias + (cr < sg ? MaxVoltage : 0);
				} else {
					return Bias;
				}
			}
			case WAVEFORM.PWM_NEGATIVE: {
				var maxwt = 2 * Math.PI * t / (64 * CircuitState.DeltaTime);
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
			//if (x < Math.PI) {
			//	return x * (2 / Math.PI) - 1;
			//}
			//return 1 - (x - Math.PI) * (2 / Math.PI);
			return x / Math.PI - 1.0;
		}

		#region [method(Analyze)]
		public override void Reset() { }

		public override void Stamp() {
			if (WaveForm == WAVEFORM.DC) {
				StampVoltageSource(NodeId[0], NodeId[1], mVoltSource, GetVoltage());
			} else {
				StampVoltageSource(NodeId[0], NodeId[1], mVoltSource);
			}
		}
		#endregion

		#region [method(Circuit)]
		public override void DoIteration() {
			if (WaveForm != WAVEFORM.DC) {
				UpdateVoltage(mVoltSource, GetVoltage());
			}
		}

		public override void FinishIteration() {
			if (WaveForm == WAVEFORM.NOISE) {
				NoiseValue = (mRandom.NextDouble() * 2 - 1) * MaxVoltage + Bias;
			}
		}
		#endregion
	}
}
