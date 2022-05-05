namespace Circuit.Elements.Passive {
    class GroundElm : BaseElement {
        public GroundElm() { }

        public override double VoltageDiff { get { return 0; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override int PostCount { get { return 1; } }

        public override bool AnaHasGroundConnection(int n1) { return true; }

        public override double GetCurrentIntoNode(int n) { return -mCurrent; }

        public override void CirSetCurrent(int x, double c) { mCurrent = -c; }

        public override void AnaStamp() {
            mCir.StampVoltageSource(0, Nodes[0], mVoltSource, 0);
        }
    }
}
