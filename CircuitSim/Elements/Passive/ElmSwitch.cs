namespace Circuit.Elements.Passive {
    class ElmSwitch : BaseElement {
        public bool Momentary;
        public int Position;
        public int PosCount;
        public int Link;

        public ElmSwitch() : base() {
            Momentary = false;
            Position = 0;
            PosCount = 2;
            Link = 0;
        }

        public ElmSwitch(bool mm) : base() {
            Position = mm ? 1 : 0;
            Momentary = mm;
            PosCount = 2;
            Link = 0;
        }

        public ElmSwitch(StringTokenizer st) : base() {
            Position = 0;
            Momentary = false;
            PosCount = 2;
            Link = 0;
            try {
                string str = st.nextToken();
                Position = int.Parse(str);
                Momentary = st.nextTokenBool();
                Link = st.nextTokenInt();
            } catch { }
        }

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
