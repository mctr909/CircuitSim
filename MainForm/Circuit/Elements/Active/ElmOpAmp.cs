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

		public override double GetVoltageDiff() {
			return NodeVolts[V_O] - NodeVolts[V_P];
		}

		#region [method(Analyze)]
		/* there is no current path through the op-amp inputs,
         * but there is an indirect path through the output to ground. */
		public override bool HasConnection(int n1, int n2) { return false; }

		public override bool HasGroundConnection(int nodeIndex) { return nodeIndex == 2; }

		public override void Stamp() {
			var vn = CircuitAnalizer.NodeCount + mVoltSource;
			StampMatrix(NodeId[2], vn, 1);
			StampNonLinear(vn);
		}
		#endregion

		#region [method(Circuit)]
		public override void DoIteration() {
			var vd = NodeVolts[V_P] - NodeVolts[V_N];
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
			var vnode = CircuitElement.VOLTAGE_SOURCE_BEGIN + mVoltSource;
			var rowV = CircuitElement.NODE_INFOS[vnode].row;
			var colri = CircuitElement.NODE_INFOS[NodeId[0] - 1];
			if (colri.is_const) {
				CircuitElement.RIGHT_SIDE[rowV] -= dx * colri.value;
			} else {
				CircuitElement.MATRIX[rowV, colri.col] += dx;
			}
			colri = CircuitElement.NODE_INFOS[NodeId[1] - 1];
			if (colri.is_const) {
				CircuitElement.RIGHT_SIDE[rowV] += dx * colri.value;
			} else {
				CircuitElement.MATRIX[rowV, colri.col] -= dx;
			}
			colri = CircuitElement.NODE_INFOS[NodeId[2] - 1];
			if (colri.is_const) {
				CircuitElement.RIGHT_SIDE[rowV] -= colri.value;
			} else {
				CircuitElement.MATRIX[rowV, colri.col] += 1;
			}
			CircuitElement.RIGHT_SIDE[rowV] += x;

			mLastVd = vd;
		}

		public override double GetCurrent(int n) {
			if (n == 2) {
				return -Current;
			}
			return 0;
		}
		#endregion
	}
}
