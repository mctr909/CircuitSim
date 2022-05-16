namespace Circuit.Elements.Passive {
    class ResistorElm : BaseElement {
        public double Resistance { get; set; }

        public ResistorElm() {
            Resistance = 1000;
        }

        public ResistorElm(StringTokenizer st) {
            Resistance = st.nextTokenDouble();
        }

        public override int PostCount { get { return 2; } }

        public override void AnaStamp() {
            Circuit.StampResistor(Nodes[0], Nodes[1], Resistance);
        }

        public override void CirSetNodeVoltage(int n, double c) {
            Volts[n] = c;
            mCurrent = (Volts[0] - Volts[1]) / Resistance;
        }
    }
}
