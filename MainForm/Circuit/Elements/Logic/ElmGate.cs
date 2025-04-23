namespace Circuit.Elements.Logic {
	abstract class ElmGate : BaseElement {
		public static double LastHighVoltage = 5;

		public int InputCount = 2;
		public bool[] InputStates;
		public bool LastOutput;
		public double HighVoltage = LastHighVoltage;
		public bool HasSchmittInputs;
		public bool IsInverting;

		int mOscillationCount;

		public override int TermCount { get { return InputCount + 1; } }

		protected bool GetInput(int x) {
			if (!HasSchmittInputs) {
				return V[x] > HighVoltage * 0.5;
			}
			bool res = V[x] > HighVoltage * (InputStates[x] ? 0.35 : 0.55);
			InputStates[x] = res;
			return res;
		}

		protected virtual bool CalcFunction() { return false; }

		protected override void DoIteration() {
			bool f = CalcFunction();
			if (IsInverting) {
				f = !f;
			}

			/* detect oscillation (using same strategy as Atanua) */
			if (LastOutput == !f) {
				if (mOscillationCount++ > 50) {
					/* output is oscillating too much, randomly leave output the same */
					mOscillationCount = 0;
					if (Random.Next(10) > 5) {
						f = LastOutput;
					}
				}
			} else {
				mOscillationCount = 0;
			}
			LastOutput = f;
			double res = f ? HighVoltage : 0;
			UpdateVoltageSource(VoltSource, res);
		}

		protected override double GetCurrent(int n) { return (n == InputCount) ? I[0] : 0; }
	}
}
