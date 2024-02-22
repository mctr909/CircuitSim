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
			CircuitElement.RowInfo[Nodes[0] - 1].LeftChanges = true;
			CircuitElement.RowInfo[Nodes[1] - 1].LeftChanges = true;
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
			var rowA = CircuitElement.RowInfo[Nodes[0] - 1].MapRow;
			var rowB = CircuitElement.RowInfo[Nodes[1] - 1].MapRow;
			var colri = CircuitElement.RowInfo[Nodes[0] - 1];
			if (colri.IsConst) {
				CircuitElement.RightSide[rowA] -= conductance * colri.Value;
				CircuitElement.RightSide[rowB] += conductance * colri.Value;
			} else {
				CircuitElement.Matrix[rowA, colri.MapCol] += conductance;
				CircuitElement.Matrix[rowB, colri.MapCol] -= conductance;
			}
			colri = CircuitElement.RowInfo[Nodes[1] - 1];
			if (colri.IsConst) {
				CircuitElement.RightSide[rowA] += conductance * colri.Value;
				CircuitElement.RightSide[rowB] -= conductance * colri.Value;
			} else {
				CircuitElement.Matrix[rowA, colri.MapCol] -= conductance;
				CircuitElement.Matrix[rowB, colri.MapCol] += conductance;
			}
		}

		public override void SetVoltage(int n, double c) {
			Volts[n] = c;
			Current = (Volts[0] - Volts[1]) / mResistance;
		}
	}
}
