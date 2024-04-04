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
			StampVoltageSource(NodeId[0], NodeId[2], mVoltSource);
			StampNonLinear(NodeId[2]);
		}

		#region [method(Circuit)]
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
			mCompResistance = CircuitState.DeltaTime / (2 * Capacitance);
			mVoltSourceValue = -CapVoltDiff - mCapCurrent * mCompResistance;
		}

		public override void DoIteration() {
			base.DoIteration();
			UpdateConductance(NodeId[2], NodeId[1], 1.0 / mCompResistance);
			var vn = CircuitElement.VOLTAGE_SOURCE_BEGIN + mVoltSource;
			CircuitElement.RIGHT_SIDE[vn] += mVoltSourceValue;
		}

		public override void SetVoltage(int nodeIndex, double v) {
			base.SetVoltage(nodeIndex, v);
			CapVoltDiff = NodeVolts[0] - NodeVolts[1];
			Current += mCapCurrent;
		}

		public override void SetCurrent(int x, double c) { mCapCurrent = c; }
		#endregion
	}
}
