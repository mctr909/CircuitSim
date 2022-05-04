namespace Circuit.Elements.Passive {
    class ResistorElmE : BaseElement {
        public double Resistance { get; set; }

        public ResistorElmE() {
            Resistance = 1000;
        }

        public ResistorElmE(double resistance) {
            Resistance = resistance;
        }

        protected override void cirCalculateCurrent() {
            mCirCurrent = (CirVolts[0] - CirVolts[1]) / Resistance;
        }

        public override void CirStamp() {
            mCir.StampResistor(CirNodes[0], CirNodes[1], Resistance);
        }
    }
}
