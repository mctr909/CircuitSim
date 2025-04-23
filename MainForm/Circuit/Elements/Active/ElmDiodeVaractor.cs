namespace Circuit.Elements.Active {
	class ElmDiodeVaractor : ElmDiode {
		public double BaseCapacitance = 4e-12;
		public double CapVoltDiff;
		public double Capacitance;

		double mCapCurrent;
		double mVoltSourceValue;
		double mCompResistance;

		protected override void StartIteration() {
			base.StartIteration();
			// capacitor companion model using trapezoidal approximation
			// (Thevenin equivalent) consists of a voltage source in
			// series with a resistor
			if (0 < CapVoltDiff) {
				Capacitance = BaseCapacitance;
			} else {
				Capacitance = BaseCapacitance / Math.Pow(1 - CapVoltDiff / FwDrop, 0.5);
			}
			mCompResistance = CircuitState.DeltaTime / (2 * Capacitance);
			mVoltSourceValue = -CapVoltDiff - mCapCurrent * mCompResistance;
		}

		protected override void DoIteration() {
			base.DoIteration();
			UpdateConductance(Nodes[2], Nodes[1], 1.0 / mCompResistance);
			var vn = VOLTAGE_SOURCE_BEGIN + VoltSource;
			RIGHTSIDE[vn] += mVoltSourceValue;
		}

		protected override void SetCurrent(int n, double i) { mCapCurrent = i; }

		public override void SetVoltage(int n, double v) {
			base.SetVoltage(n, v);
			CapVoltDiff = V[0] - V[1];
			I[0] += mCapCurrent;
		}
	}
}
