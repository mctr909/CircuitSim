namespace Circuit.Elements.Active {
	class ElmAnalogSwitch : BaseElement {
		public double Ron = 100;
		public double Roff = 1e8;
		public bool Invert;
		public bool IsOpen;

		double mResistance;

		public override int TermCount { get { return 3; } }

		public override bool has_connection(int n1, int n2) { return !(n1 == 2 || n2 == 2); }

		public override void stamp() {
			CircuitElement.NodeInfo[node_index[0] - 1].left_changes = true;
			CircuitElement.NodeInfo[node_index[1] - 1].left_changes = true;
		}

		#region [method(Circuit)]
		public override void do_iteration() {
			IsOpen = volts[2] < 2.5;
			if (Invert) {
				IsOpen = !IsOpen;
			}
			mResistance = IsOpen ? Roff : Ron;
			var conductance = 1.0 / mResistance;
			var rowA = CircuitElement.NodeInfo[node_index[0] - 1].ROW;
			var rowB = CircuitElement.NodeInfo[node_index[1] - 1].ROW;
			var colri = CircuitElement.NodeInfo[node_index[0] - 1];
			if (colri.is_const) {
				CircuitElement.RightSide[rowA] -= conductance * colri.value;
				CircuitElement.RightSide[rowB] += conductance * colri.value;
			} else {
				CircuitElement.Matrix[rowA, colri.COL] += conductance;
				CircuitElement.Matrix[rowB, colri.COL] -= conductance;
			}
			colri = CircuitElement.NodeInfo[node_index[1] - 1];
			if (colri.is_const) {
				CircuitElement.RightSide[rowA] += conductance * colri.value;
				CircuitElement.RightSide[rowB] -= conductance * colri.value;
			} else {
				CircuitElement.Matrix[rowA, colri.COL] -= conductance;
				CircuitElement.Matrix[rowB, colri.COL] += conductance;
			}
		}

		public override double get_current_into_node(int n) {
			if (n == 0) {
				return -current;
			}
			if (n == 2) {
				return 0;
			}
			return current;
		}

		public override void set_voltage(int n, double c) {
			volts[n] = c;
			current = (volts[0] - volts[1]) / mResistance;
		}
		#endregion
	}
}
