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

		public override void reset() {
			base.reset();
			CapVoltDiff = 0;
		}

		public override void stamp() {
			base.stamp();
			var n0 = node_index[0] - 1;
			var n1 = node_index[2] - 1;
			int vn = CircuitElement.NodeCount + m_volt_source - 1;
			CircuitElement.Matrix[vn, n0] -= 1;
			CircuitElement.Matrix[vn, n1] += 1;
			CircuitElement.Matrix[n0, vn] += 1;
			CircuitElement.Matrix[n1, vn] -= 1;
			CircuitElement.NodeInfo[vn].right_changes = true;
			CircuitElement.NodeInfo[n1].left_changes = true;
		}

		#region [method(Circuit)]
		public override void prepare_iteration() {
			base.prepare_iteration();
			// capacitor companion model using trapezoidal approximation
			// (Thevenin equivalent) consists of a voltage source in
			// series with a resistor
			if (0 < CapVoltDiff) {
				Capacitance = BaseCapacitance;
			} else {
				Capacitance = BaseCapacitance / Math.Pow(1 - CapVoltDiff / FwDrop, 0.5);
			}
			mCompResistance = CircuitElement.DeltaTime / (2 * Capacitance);
			mVoltSourceValue = -CapVoltDiff - mCapCurrent * mCompResistance;
		}

		public override void do_iteration() {
			base.do_iteration();
			var g = 1.0 / mCompResistance;
			var n0 = node_index[2] - 1;
			var n1 = node_index[1] - 1;
			var vn = CircuitElement.NodeCount + m_volt_source - 1;
			CircuitElement.Matrix[n0, n0] += g;
			CircuitElement.Matrix[n1, n1] += g;
			CircuitElement.Matrix[n0, n1] -= g;
			CircuitElement.Matrix[n1, n0] -= g;
			CircuitElement.RightSide[vn] += mVoltSourceValue;
		}

		public override void set_voltage(int n, double c) {
			base.set_voltage(n, c);
			CapVoltDiff = volts[0] - volts[1];
			current += mCapCurrent;
		}

		public override void set_current(int x, double c) { mCapCurrent = c; }
		#endregion
	}
}
