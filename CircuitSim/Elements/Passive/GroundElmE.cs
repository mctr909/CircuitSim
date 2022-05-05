namespace Circuit.Elements.Passive {
    class GroundElmE : BaseElement {
        public GroundElmE() { }

        public override double VoltageDiff { get { return 0; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override int PostCount { get { return 1; } }

        public override bool HasGroundConnection(int n1) { return true; }

        public override double GetCurrentIntoNode(int n) { return -mCurrent; }

        public override void SetCurrent(int x, double c) { mCurrent = -c; }

        public override void Stamp() {
            mCir.StampVoltageSource(0, Nodes[0], mVoltSource, 0);
        }
    }
}
