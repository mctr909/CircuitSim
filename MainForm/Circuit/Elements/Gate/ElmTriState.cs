namespace Circuit.Elements.Gate {
	class ElmTriState : BaseElement {
		double mResistance;
		public double Ron = 0.1;
		public double Roff = 1e10;

		public bool Open { get; private set; }

		public override int TermCount { get { return 3; } }

		public override int InternalNodeCount { get { return 1; } }

		public override int VoltageSourceCount { get { return 1; } }

		public override double GetCurrentIntoNode(int n) {
			if (n == 1) {
				return Current;
			}
			return 0;
		}

		/* there is no current path through the input, but there
         * is an indirect path through the output to ground. */
		public override bool GetConnection(int n1, int n2) { return false; }

		public override bool HasGroundConnection(int n1) {
			return n1 == 1;
		}

		public override void Stamp() {
			CircuitElement.StampVoltageSource(0, Nodes[3], mVoltSource);
			CircuitElement.StampNonLinear(Nodes[3]);
			CircuitElement.StampNonLinear(Nodes[1]);
		}

		public override void DoIteration() {
			Open = Volts[2] < 2.5;
			mResistance = Open ? Roff : Ron;
			CircuitElement.StampResistor(Nodes[3], Nodes[1], mResistance);
			CircuitElement.UpdateVoltageSource(mVoltSource, Volts[0] > 2.5 ? 5 : 0);
		}

		public override void SetVoltage(int n, double c) {
			Volts[n] = c;
			Current = (Volts[0] - Volts[1]) / mResistance;
		}
	}
}
