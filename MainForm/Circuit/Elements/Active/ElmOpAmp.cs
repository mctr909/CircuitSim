namespace Circuit.Elements.Active {
	class ElmOpAmp : BaseElement {
		public const int V_N = 0;
		public const int V_P = 1;
		public const int V_O = 2;

		public double MaxOut = 15;
		public double MinOut = -15;
		public double Gain = 100000;

		double mLastVd;

		public override int VoltageSourceCount { get { return 1; } }

		public override int TermCount { get { return 3; } }

		public override double VoltageDiff { get { return Volts[V_O] - Volts[V_P]; } }

		/* there is no current path through the op-amp inputs,
         * but there is an indirect path through the output to ground. */
		public override bool GetConnection(int n1, int n2) { return false; }

		public override bool HasGroundConnection(int n1) { return n1 == 2; }

		public override void Stamp() {
			int vn = CircuitElement.Nodes.Count + mVoltSource;
			CircuitElement.StampNonLinear(vn);
			CircuitElement.StampMatrix(Nodes[2], vn, 1);
		}

		public override double GetCurrentIntoNode(int n) {
			if (n == 2) {
				return -Current;
			}
			return 0;
		}

		public override void DoIteration() {
			var vd = Volts[V_P] - Volts[V_N];
			double dx;
			double x;
			if (vd >= MaxOut / Gain && (mLastVd >= 0 || mRandom.Next(4) == 1)) {
				dx = 1e-4;
				x = MaxOut - dx * MaxOut / Gain;
			} else if (vd <= MinOut / Gain && (mLastVd <= 0 || mRandom.Next(4) == 1)) {
				dx = 1e-4;
				x = MinOut - dx * MinOut / Gain;
			} else {
				dx = Gain;
				x = 0;
			}

			/* newton-raphson */
			var vnode = CircuitElement.Nodes.Count + mVoltSource;
			var rowV = CircuitElement.RowInfo[vnode - 1].MapRow;
			var colri = CircuitElement.RowInfo[Nodes[0] - 1];
			if (colri.IsConst) {
				CircuitElement.RightSide[rowV] -= dx * colri.Value;
			} else {
				CircuitElement.Matrix[rowV, colri.MapCol] += dx;
			}
			colri = CircuitElement.RowInfo[Nodes[1] - 1];
			if (colri.IsConst) {
				CircuitElement.RightSide[rowV] += dx * colri.Value;
			} else {
				CircuitElement.Matrix[rowV, colri.MapCol] -= dx;
			}
			colri = CircuitElement.RowInfo[Nodes[2] - 1];
			if (colri.IsConst) {
				CircuitElement.RightSide[rowV] -= colri.Value;
			} else {
				CircuitElement.Matrix[rowV, colri.MapCol] += 1;
			}
			CircuitElement.RightSide[rowV] += x;

			mLastVd = vd;
		}
	}
}
