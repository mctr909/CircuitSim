namespace Circuit.Elements.Passive {
    class SwitchElm : BaseElement {
        public bool Momentary;
        public int Position;
        public int PosCount;

        public SwitchElm() : base() {
            Momentary = false;
            Position = 0;
            PosCount = 2;
        }

        public SwitchElm(bool mm) : base() {
            Position = mm ? 1 : 0;
            Momentary = mm;
            PosCount = 2;
        }

        public SwitchElm(StringTokenizer st) : base() {
            Position = 0;
            Momentary = false;
            PosCount = 2;
            try {
                string str = st.nextToken();
                Position = int.Parse(str);
                Momentary = st.nextTokenBool();
            } catch { }
        }

        public override int PostCount { get { return 2; } }
        public override bool IsWire { get { return Position == 0; } }
        public override int VoltageSourceCount { get { return (1 == Position) ? 0 : 1; } }

        public override bool GetConnection(int n1, int n2) { return 0 == Position; }

        public override void AnaStamp() {
            if (Position == 0) {
                mCir.StampVoltageSource(Nodes[0], Nodes[1], mVoltSource, 0);
            }
        }

        public override void CirSetNodeVoltage(int n, double c) {
            Volts[n] = c;
            if (Position == 1) {
                mCurrent = 0;
            }
        }
    }
}
