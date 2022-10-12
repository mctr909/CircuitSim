namespace Circuit.Elements.Passive {
    class ElmWire : BaseElement {
        public ElmWire() { }

        public override int PostCount { get { return 2; } }

        public override bool IsWire { get { return true; } }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override double Power { get { return 0; } }

        public override void CirSetVoltage(int n, double c) {
            Volts[n] = c;
        }
    }
}
