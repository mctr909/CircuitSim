namespace Circuit.Elements.Gate {
	class ElmGate : BaseElement {
		public static double LastHighVoltage = 5;

		public int InputCount = 2;
		public bool[] InputStates;
		public bool LastOutput;
		public double HighVoltage;
		public bool HasSchmittInputs;
		public bool IsInverting;

		int mOscillationCount;

		public ElmGate() : base() {
			InputCount = 2;
			/* copy defaults from last gate edited */
			HighVoltage = LastHighVoltage;
		}

		public override int VoltageSourceCount { get { return 1; } }

		public override int TermCount { get { return InputCount + 1; } }

		public override double GetCurrentIntoNode(int n) {
			if (n == InputCount) {
				return Current;
			}
			return 0;
		}

		/* there is no current path through the gate inputs,
        * but there is an indirect path through the output to ground. */
		public override bool GetConnection(int n1, int n2) { return false; }

		public override bool HasGroundConnection(int n1) {
			return (n1 == InputCount);
		}

		public override void Stamp() {
			CircuitElement.StampVoltageSource(0, Nodes[InputCount], mVoltSource);
		}

		public override void DoIteration() {
			bool f = calcFunction();
			if (IsInverting) {
				f = !f;
			}

			/* detect oscillation (using same strategy as Atanua) */
			if (LastOutput == !f) {
				if (mOscillationCount++ > 50) {
					/* output is oscillating too much, randomly leave output the same */
					mOscillationCount = 0;
					if (mRandom.Next(10) > 5) {
						f = LastOutput;
					}
				}
			} else {
				mOscillationCount = 0;
			}
			LastOutput = f;
			double res = f ? HighVoltage : 0;
			CircuitElement.UpdateVoltageSource(mVoltSource, res);
		}

		protected bool getInput(int x) {
			if (!HasSchmittInputs) {
				return Volts[x] > HighVoltage * 0.5;
			}
			bool res = Volts[x] > HighVoltage * (InputStates[x] ? 0.35 : 0.55);
			InputStates[x] = res;
			return res;
		}

		protected virtual bool calcFunction() { return false; }
	}
}
