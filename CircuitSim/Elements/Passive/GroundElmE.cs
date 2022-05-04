namespace Circuit.Elements.Passive {
    class GroundElmE : BaseElement {
        public GroundElmE() { }

        public override double CirVoltageDiff { get { return 0; } }

        public override int CirVoltageSourceCount { get { return 1; } }

        public override int CirPostCount { get { return 1; } }

        public override bool CirHasGroundConnection(int n1) { return true; }

        public override double CirGetCurrentIntoNode(int n) { return -mCirCurrent; }

        public override void CirSetCurrent(int x, double c) { mCirCurrent = -c; }

        public override void CirStamp() {
            mCir.StampVoltageSource(0, CirNodes[0], mCirVoltSource, 0);
        }
    }
}
