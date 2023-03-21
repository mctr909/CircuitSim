namespace Circuit.Elements.Passive {
    class ElmSwitch : BaseElement {
        public bool Momentary = false;
        public int Position = 0;
        public int PosCount = 2;
        public int Link = 0;

        public override int PostCount { get { return 2; } }
        public override bool IsWire { get { return Position == 0; } }
        public override int AnaVoltageSourceCount { get { return (1 == Position) ? 0 : 1; } }

        public override bool AnaGetConnection(int n1, int n2) { return 0 == Position; }

        public override void AnaStamp() {
            if (Position == 0) {
                var n0 = Nodes[0] - 1;
                var n1 = Nodes[1] - 1;
                int vn = Circuit.Nodes.Count + mVoltSource - 1;
                Circuit.Matrix[vn, n0] -= 1;
                Circuit.Matrix[vn, n1] += 1;
                Circuit.Matrix[n0, vn] += 1;
                Circuit.Matrix[n1, vn] -= 1;
            }
        }

        public override void CirSetVoltage(int n, double c) {
            Volts[n] = c;
            if (Position == 1) {
                Current = 0;
            }
        }
    }
}
