namespace Circuit.Elements.Passive {
    class ElmGround : BaseElement {
        public override double VoltageDiff { get { return 0; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override int PostCount { get { return 1; } }

        public override double GetCurrentIntoNode(int n) { return -Current; }

        public override bool AnaHasGroundConnection(int n1) { return true; }

        public override void AnaStamp() {
            var nv = Circuit.NodeList.Count + mVoltSource - 1;
            var n0 = Nodes[0] - 1;
            Circuit.Matrix[nv, n0] += 1;
            Circuit.Matrix[n0, nv] -= 1;
        }

        public override void CirSetCurrent(int x, double c) { Current = -c; }
    }
}
