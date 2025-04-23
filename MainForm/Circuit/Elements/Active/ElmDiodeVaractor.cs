namespace Circuit.Elements.Active {
	class ElmDiodeVaractor : ElmDiode {
		protected override void StartIteration() {
			base.StartIteration();
			// capacitor companion model using trapezoidal approximation
			// (Thevenin equivalent) consists of a voltage source in
			// series with a resistor
			double cap;
			if (0 < V[VD_CAP]) {
				cap = Para[CAPACITANCE];
			} else {
				cap = Para[CAPACITANCE] / Math.Pow(1 - V[VD_CAP] / Para[FW_DROP], 0.5);
			}
			Para[RESISTANCE] = CircuitState.DeltaTime / (2 * cap);
			V[VS_CAP] = -V[VD_CAP] - I[CUR_CAP] * Para[RESISTANCE];
		}

		protected override void DoIteration() {
			base.DoIteration();
			UpdateConductance(Nodes[2], Nodes[1], 1.0 / Para[RESISTANCE]);
			var vn = VOLTAGE_SOURCE_BEGIN + VoltSource;
			RIGHTSIDE[vn] += V[VS_CAP];
		}

		protected override void SetCurrent(int n, double i) { I[CUR_CAP] = i; }

		public override void SetVoltage(int n, double v) {
			base.SetVoltage(n, v);
			V[VD_CAP] = V[0] - V[1];
			I[0] += I[CUR_CAP];
		}
	}
}
