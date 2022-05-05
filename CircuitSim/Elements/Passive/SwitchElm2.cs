namespace Circuit.Elements.Passive {
    class SwitchElm2 : SwitchElm {
        public int mLink;
        public int mThrowCount;

        public SwitchElm2() : base(false) {
            mThrowCount = 2;
            AllocNodes();
        }

        public SwitchElm2(StringTokenizer st) : base(st) {
            mLink = st.nextTokenInt();
            mThrowCount = 2;
            try {
                mThrowCount = st.nextTokenInt();
            } catch { }
            AllocNodes();
        }

        public override bool IsWire { get { return true; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override int PostCount { get { return 1 + mThrowCount; } }

        protected override void calcCurrent() { }

        public override double GetCurrentIntoNode(int n) {
            if (n == 0) {
                return -mCurrent;
            }
            if (n == Position + 1) {
                return mCurrent;
            }
            return 0;
        }

        public override void Stamp() {
            mCir.StampVoltageSource(Nodes[0], Nodes[Position + 1], mVoltSource, 0);
        }
    }
}
