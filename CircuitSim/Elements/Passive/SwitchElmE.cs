namespace Circuit.Elements.Passive {
    class SwitchElmE : BaseElement {
        public bool Momentary;
        public int Position;
        public int PosCount;

        public SwitchElmE() : base() {
            Momentary = false;
            Position = 0;
            PosCount = 2;
        }

        public SwitchElmE(bool mm) : base() {
            Position = mm ? 1 : 0;
            Momentary = mm;
            PosCount = 2;
        }

        public SwitchElmE(StringTokenizer st) : base() {
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

        protected override void calcCurrent() {
            if (Position == 1) {
                mCurrent = 0;
            }
        }

        public override void Stamp() {
            if (Position == 0) {
                mCir.StampVoltageSource(Nodes[0], Nodes[1], mVoltSource, 0);
            }
        }
    }
}
