﻿namespace Circuit.Elements.Active {
    class ElmAnalogSwitch : BaseElement {
        public double Ron = 20;
        public double Roff = 1e10;
        public bool Invert;

        double mResistance;

        public bool IsOpen { get; private set; }

        public override int TermCount { get { return 3; } }

        public override bool AnaGetConnection(int n1, int n2) { return !(n1 == 2 || n2 == 2); }

        public override void AnaStamp() {
            Circuit.RowInfo[Nodes[0] - 1].LeftChanges = true;
            Circuit.RowInfo[Nodes[1] - 1].LeftChanges = true;
        }

        public override double CirGetCurrentIntoNode(int n) {
            if (n == 0) {
                return -Current;
            }
            if (n == 2) {
                return 0;
            }
            return Current;
        }

        public override void CirDoIteration() {
            IsOpen = Volts[2] < 2.5;
            if (Invert) {
                IsOpen = !IsOpen;
            }
            mResistance = IsOpen ? Roff : Ron;
            var conductance = 1.0 / mResistance;
            var rowA = Circuit.RowInfo[Nodes[0] - 1].MapRow;
            var rowB = Circuit.RowInfo[Nodes[1] - 1].MapRow;
            var colri = Circuit.RowInfo[Nodes[0] - 1];
            if (colri.IsConst) {
                Circuit.RightSide[rowA] -= conductance * colri.Value;
                Circuit.RightSide[rowB] += conductance * colri.Value;
            } else {
                Circuit.Matrix[rowA, colri.MapCol] += conductance;
                Circuit.Matrix[rowB, colri.MapCol] -= conductance;
            }
            colri = Circuit.RowInfo[Nodes[1] - 1];
            if (colri.IsConst) {
                Circuit.RightSide[rowA] += conductance * colri.Value;
                Circuit.RightSide[rowB] -= conductance * colri.Value;
            } else {
                Circuit.Matrix[rowA, colri.MapCol] -= conductance;
                Circuit.Matrix[rowB, colri.MapCol] += conductance;
            }
        }

        public override void CirSetVoltage(int n, double c) {
            Volts[n] = c;
            Current = (Volts[0] - Volts[1]) / mResistance;
        }
    }
}
