namespace Circuit.Elements.Passive {
    class Switch2ElmE : SwitchElmE {
        public int mLink;
        public int mThrowCount;

        public Switch2ElmE() : base(false) {
            mThrowCount = 2;
            cirAllocNodes();
        }

        public Switch2ElmE(StringTokenizer st) : base(st) {
            mLink = st.nextTokenInt();
            mThrowCount = 2;
            try {
                mThrowCount = st.nextTokenInt();
            } catch { }
            cirAllocNodes();
        }

        public override bool CirIsWire { get { return true; } }

        public override int CirVoltageSourceCount { get { return 1; } }

        public override int CirPostCount { get { return 1 + mThrowCount; } }

        protected override void cirCalculateCurrent() { }

        public override double CirGetCurrentIntoNode(int n) {
            if (n == 0) {
                return -mCirCurrent;
            }
            if (n == Position + 1) {
                return mCirCurrent;
            }
            return 0;
        }

        public override void CirStamp() {
            mCir.StampVoltageSource(CirNodes[0], CirNodes[Position + 1], mCirVoltSource, 0);
        }
    }
}
