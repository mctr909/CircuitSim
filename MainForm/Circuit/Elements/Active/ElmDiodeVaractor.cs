namespace Circuit.Elements.Active {
	class ElmDiodeVaractor : ElmDiode {
		public double BaseCapacitance;
		public double Capacitance;
		public double CapVoltDiff;

		double mCapCurrent;
		double mVoltSourceValue;
		// DiodeElm.lastvoltdiff = volt diff from last iteration
		// capvoltdiff = volt diff from last timestep
		double mCompResistance;

		public ElmDiodeVaractor() : base() {
			BaseCapacitance = 4e-12;
		}

		public override int VoltageSourceCount { get { return 1; } }

		public override int InternalNodeCount { get { return 1; } }

		public override void Reset() {
			base.Reset();
			CapVoltDiff = 0;
		}

		public override void Stamp() {
			base.Stamp();
			var n0 = Nodes[0] - 1;
			var n1 = Nodes[2] - 1;
			int vn = CircuitElement.Nodes.Count + mVoltSource - 1;
			CircuitElement.Matrix[vn, n0] -= 1;
			CircuitElement.Matrix[vn, n1] += 1;
			CircuitElement.Matrix[n0, vn] += 1;
			CircuitElement.Matrix[n1, vn] -= 1;
			CircuitElement.RowInfo[vn].RightChanges = true;
			CircuitElement.RowInfo[n1].LeftChanges = true;
		}

		public override void DoIteration() {
			base.DoIteration();
			var g = 1.0 / mCompResistance;
			var n0 = Nodes[2] - 1;
			var n1 = Nodes[1] - 1;
			var vn = CircuitElement.Nodes.Count + mVoltSource - 1;
			CircuitElement.Matrix[n0, n0] += g;
			CircuitElement.Matrix[n1, n1] += g;
			CircuitElement.Matrix[n0, n1] -= g;
			CircuitElement.Matrix[n1, n0] -= g;
			CircuitElement.RightSide[vn] += mVoltSourceValue;
		}

		public override void PrepareIteration() {
			base.PrepareIteration();
			// capacitor companion model using trapezoidal approximation
			// (Thevenin equivalent) consists of a voltage source in
			// series with a resistor
			double c0 = BaseCapacitance;
			if (0 < CapVoltDiff) {
				Capacitance = c0;
			} else {
				Capacitance = c0 / Math.Pow(1 - CapVoltDiff / FwDrop, 0.5);
			}
			mCompResistance = CircuitElement.TimeStep / (2 * Capacitance);
			mVoltSourceValue = -CapVoltDiff - mCapCurrent * mCompResistance;
		}

		public override void SetVoltage(int n, double c) {
			base.SetVoltage(n, c);
			CapVoltDiff = Volts[0] - Volts[1];
			Current += mCapCurrent;
		}

		public override void SetCurrent(int x, double c) { mCapCurrent = c; }
	}
}
