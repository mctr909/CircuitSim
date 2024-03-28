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

		public override double voltage_diff() {
			return volts[V_O] - volts[V_P];
		}

		#region [method(Analyze)]
		/* there is no current path through the op-amp inputs,
         * but there is an indirect path through the output to ground. */
		public override bool has_connection(int n1, int n2) { return false; }

		public override bool has_ground_connection(int n1) { return n1 == 2; }

		public override void stamp() {
			int vn = CircuitElement.nodes.Length + m_volt_source;
			CircuitElement.StampNonLinear(vn);
			CircuitElement.StampMatrix(node_index[2], vn, 1);
		}
		#endregion

		#region [method(Circuit)]
		public override void do_iteration() {
			var vd = volts[V_P] - volts[V_N];
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
			var vnode = CircuitElement.nodes.Length + m_volt_source;
			var rowV = CircuitElement.row_info[vnode - 1].row;
			var colri = CircuitElement.row_info[node_index[0] - 1];
			if (colri.is_const) {
				CircuitElement.right_side[rowV] -= dx * colri.value;
			} else {
				CircuitElement.matrix[rowV, colri.col] += dx;
			}
			colri = CircuitElement.row_info[node_index[1] - 1];
			if (colri.is_const) {
				CircuitElement.right_side[rowV] += dx * colri.value;
			} else {
				CircuitElement.matrix[rowV, colri.col] -= dx;
			}
			colri = CircuitElement.row_info[node_index[2] - 1];
			if (colri.is_const) {
				CircuitElement.right_side[rowV] -= colri.value;
			} else {
				CircuitElement.matrix[rowV, colri.col] += 1;
			}
			CircuitElement.right_side[rowV] += x;

			mLastVd = vd;
		}

		public override double get_current_into_node(int n) {
			if (n == 2) {
				return -current;
			}
			return 0;
		}
		#endregion
	}
}
