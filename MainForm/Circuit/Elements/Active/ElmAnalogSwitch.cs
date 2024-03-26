namespace Circuit.Elements.Active {
	class ElmAnalogSwitch : BaseElement {
		public double Ron = 100;
		public double Roff = 1e8;
		public bool Invert;
		public bool IsOpen;

		double mResistance;

		public override int TermCount { get { return 3; } }

		public override bool GetConnection(int n1, int n2) { return !(n1 == 2 || n2 == 2); }

		public override void Stamp() {
			CircuitElement.row_info[Nodes[0] - 1].left_changes = true;
			CircuitElement.row_info[Nodes[1] - 1].left_changes = true;
		}

		public override double GetCurrentIntoNode(int n) {
			if (n == 0) {
				return -Current;
			}
			if (n == 2) {
				return 0;
			}
			return Current;
		}

		public override void DoIteration() {
			IsOpen = Volts[2] < 2.5;
			if (Invert) {
				IsOpen = !IsOpen;
			}
			mResistance = IsOpen ? Roff : Ron;
			var conductance = 1.0 / mResistance;
			var rowA = CircuitElement.row_info[Nodes[0] - 1].row;
			var rowB = CircuitElement.row_info[Nodes[1] - 1].row;
			var colri = CircuitElement.row_info[Nodes[0] - 1];
			if (colri.is_const) {
				CircuitElement.right_side[rowA] -= conductance * colri.value;
				CircuitElement.right_side[rowB] += conductance * colri.value;
			} else {
				CircuitElement.matrix[rowA, colri.col] += conductance;
				CircuitElement.matrix[rowB, colri.col] -= conductance;
			}
			colri = CircuitElement.row_info[Nodes[1] - 1];
			if (colri.is_const) {
				CircuitElement.right_side[rowA] += conductance * colri.value;
				CircuitElement.right_side[rowB] -= conductance * colri.value;
			} else {
				CircuitElement.matrix[rowA, colri.col] -= conductance;
				CircuitElement.matrix[rowB, colri.col] += conductance;
			}
		}

		public override void SetVoltage(int n, double c) {
			Volts[n] = c;
			Current = (Volts[0] - Volts[1]) / mResistance;
		}
	}
}
