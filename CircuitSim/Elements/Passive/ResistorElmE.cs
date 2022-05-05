namespace Circuit.Elements.Passive {
    class ResistorElmE : BaseElement {
        public double Resistance { get; set; }

        public ResistorElmE() {
            Resistance = 1000;
        }

        public ResistorElmE(double resistance) {
            Resistance = resistance;
        }

        public override int PostCount { get { return 2; } }

        protected override void calcCurrent() {
            mCurrent = (Volts[0] - Volts[1]) / Resistance;
        }

        public override void Stamp() {
            mCir.StampResistor(Nodes[0], Nodes[1], Resistance);
        }
    }
}
