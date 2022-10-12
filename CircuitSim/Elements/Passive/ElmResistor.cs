namespace Circuit.Elements.Passive {
    class ElmResistor : BaseElement {
        public double Resistance { get; set; }

        public ElmResistor() {
            Resistance = 1000;
        }

        public ElmResistor(double resistance) {
            Resistance = resistance;
        }

        public override int PostCount { get { return 2; } }

        public override void AnaStamp() {
            Circuit.StampResistor(Nodes[0], Nodes[1], Resistance);
        }

        public override void CirSetVoltage(int n, double c) {
            Volts[n] = c;
            mCurrent = (Volts[0] - Volts[1]) / Resistance;
        }
    }
}
