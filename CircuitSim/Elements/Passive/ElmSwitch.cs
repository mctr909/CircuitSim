namespace Circuit.Elements.Passive {
    class ElmSwitch : BaseElement {
        public bool Momentary = false;
        public int Position = 0;
        public int PosCount = 2;
        public int Link = 0;

        public override int PostCount { get { return 2; } }
        public override bool IsWire { get { return Position == 0; } }
        public override int VoltageSourceCount { get { return (1 == Position) ? 0 : 1; } }

        public override bool AnaGetConnection(int n1, int n2) { return 0 == Position; }

        public override void AnaStamp() {
            if (Position == 0) {
                Circuit.StampVoltageSource(Nodes[0], Nodes[1], mVoltSource, 0);
            }
        }

        public override void CirSetVoltage(int n, double c) {
            Volts[n] = c;
            if (Position == 1) {
                mCurrent = 0;
            }
        }
    }
}
