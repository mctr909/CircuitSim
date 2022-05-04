namespace Circuit.Elements.Passive {
    class WireElmE : BaseElement {
        public WireElmE() { }

        public override bool CirIsWire { get { return true; } }

        public override double CirVoltageDiff { get { return CirVolts[0]; } }

        public override double CirPower { get { return 0; } }
    }
}
