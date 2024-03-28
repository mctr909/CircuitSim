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

		public override int VoltageSourceCount { get { return 1; } }

		public override int TermCount { get { return InputCount + 1; } }

		protected bool GetInput(int x) {
			if (!HasSchmittInputs) {
				return volts[x] > HighVoltage * 0.5;
			}
			bool res = volts[x] > HighVoltage * (InputStates[x] ? 0.35 : 0.55);
			InputStates[x] = res;
			return res;
		}

		protected virtual bool CalcFunction() { return false; }

		#region [method(Analyze)]
		/* there is no current path through the gate inputs,
        * but there is an indirect path through the output to ground. */
		public override bool has_connection(int n1, int n2) { return false; }

		public override bool has_ground_connection(int n1) {
			return (n1 == InputCount);
		}

		public override void stamp() {
			CircuitElement.StampVoltageSource(0, node_index[InputCount], m_volt_source);
		}
		#endregion

		#region [method(Circuit)]
		public override void do_iteration() {
			bool f = CalcFunction();
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
			CircuitElement.UpdateVoltageSource(m_volt_source, res);
		}

		public override double get_current_into_node(int n) {
			if (n == InputCount) {
				return current;
			}
			return 0;
		}
		#endregion
	}
}
