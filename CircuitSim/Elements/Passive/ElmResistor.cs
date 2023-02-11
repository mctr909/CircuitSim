namespace Circuit.Elements.Passive {
    class ElmResistor : BaseElement {
        public double Resistance = 1000;

        public override int PostCount { get { return 2; } }

        public override void AnaStamp() {
            var g = 1.0 / Resistance;
            var n0 = Nodes[0] - 1;
            var n1 = Nodes[1] - 1;
            Circuit.Matrix[n0, n0] += g;
            Circuit.Matrix[n1, n1] += g;
            Circuit.Matrix[n0, n1] -= g;
            Circuit.Matrix[n1, n0] -= g;
        }

        public override void CirSetVoltage(int n, double c) {
            Volts[n] = c;
            Current = (Volts[0] - Volts[1]) / Resistance;
        }
    }
}
