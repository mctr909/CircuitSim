namespace Circuit.Elements.Active {
	class ElmDiodeVaractor : ElmDiode {
		public double BaseCapacitance = 4e-12;
		public double CapVoltDiff;
		public double Capacitance;

		double mCapCurrent;
		double mVoltSourceValue;
		double mCompResistance;

		public override int VoltageSourceCount { get { return 1; } }

		public override int InternalNodeCount { get { return 1; } }

		public ElmDiodeVaractor() : base() { }

		public override void Reset() {
			base.Reset();
			CapVoltDiff = 0;
		}

		public override void Stamp() {
			base.Stamp();
			var n0 = Nodes[0] - 1;
			var n1 = Nodes[2] - 1;
			int vn = CircuitElement.nodes.Length + mVoltSource - 1;
			CircuitElement.matrix[vn, n0] -= 1;
			CircuitElement.matrix[vn, n1] += 1;
			CircuitElement.matrix[n0, vn] += 1;
			CircuitElement.matrix[n1, vn] -= 1;
			CircuitElement.row_info[vn].right_changes = true;
			CircuitElement.row_info[n1].left_changes = true;
		}

		public override void DoIteration() {
			base.DoIteration();
			var g = 1.0 / mCompResistance;
			var n0 = Nodes[2] - 1;
			var n1 = Nodes[1] - 1;
			var vn = CircuitElement.nodes.Length + mVoltSource - 1;
			CircuitElement.matrix[n0, n0] += g;
			CircuitElement.matrix[n1, n1] += g;
			CircuitElement.matrix[n0, n1] -= g;
			CircuitElement.matrix[n1, n0] -= g;
			CircuitElement.right_side[vn] += mVoltSourceValue;
		}

		public override void PrepareIteration() {
			base.PrepareIteration();
			// capacitor companion model using trapezoidal approximation
			// (Thevenin equivalent) consists of a voltage source in
			// series with a resistor
			if (0 < CapVoltDiff) {
				Capacitance = BaseCapacitance;
			} else {
				Capacitance = BaseCapacitance / Math.Pow(1 - CapVoltDiff / FwDrop, 0.5);
			}
			mCompResistance = CircuitElement.delta_time / (2 * Capacitance);
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
