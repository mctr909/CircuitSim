namespace Circuit.Elements.Passive {
    class ElmSwitchMulti : ElmSwitch {
        public int ThrowCount = 1;

        public override bool IsWire { get { return true; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override int PostCount { get { return 1 + ThrowCount; } }

        public override double GetCurrentIntoNode(int n) {
            if (n == 0) {
                return -mCurrent;
            }
            if (n == Position + 1) {
                return mCurrent;
            }
            return 0;
        }

        public override bool AnaGetConnection(int n1, int n2) {
            return ComparePair(n1, n2, 0, 1 + Position);
        }

        public override void AnaStamp() {
            Circuit.StampVoltageSource(Nodes[0], Nodes[Position + 1], mVoltSource, 0);
        }
    }
}
