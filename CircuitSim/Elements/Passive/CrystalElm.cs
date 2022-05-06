using Circuit.Elements.Custom;

namespace Circuit.Elements.Passive {
    class CrystalElm : CompositeElm {
        static readonly int[] EXTERNAL_NODES = { 1, 2 };
        static readonly string MODEL_STRING
            = ELEMENTS.CAPACITOR + " 1 2\r"
            + ELEMENTS.CAPACITOR + " 1 3\r"
            + ELEMENTS.INDUCTOR + " 3 4\r"
            + ELEMENTS.RESISTOR + " 4 2";

        public double SeriesCapacitance;
        public double ParallelCapacitance;
        public double Inductance;
        public double Resistance;

        public CrystalElm() : base(MODEL_STRING, EXTERNAL_NODES) {
            ParallelCapacitance = 28.7e-12;
            SeriesCapacitance = 0.1e-12;
            Inductance = 2.5e-3;
            Resistance = 6.4;
            initCrystal();
        }

        public CrystalElm(StringTokenizer st) : base(st, MODEL_STRING, EXTERNAL_NODES) {
            var c1 = (CapacitorElm)compElmList[0].CirElm;
            ParallelCapacitance = c1.Capacitance;
            var c2 = (CapacitorElm)compElmList[1].CirElm;
            SeriesCapacitance = c2.Capacitance;
            var i1 = (InductorElm)compElmList[2].CirElm;
            Inductance = i1.Inductance;
            var r1 = (ResistorElm)compElmList[3].CirElm;
            Resistance = r1.Resistance;
            initCrystal();
        }

        public void initCrystal() {
            var c1 = (CapacitorElm)compElmList[0].CirElm;
            c1.Capacitance = ParallelCapacitance;
            var c2 = (CapacitorElm)compElmList[1].CirElm;
            c2.Capacitance = SeriesCapacitance;
            var i1 = (InductorElm)compElmList[2].CirElm;
            i1.Inductance = Inductance;
            var r1 = (ResistorElm)compElmList[3].CirElm;
            r1.Resistance = Resistance;
        }

        public override void CirSetNodeVoltage(int n, double c) {
            base.CirSetNodeVoltage(n, c);
            if (Volts.Length <= n) {
                return;
            }
            Volts[n] = c;
            mCurrent = GetCurrentIntoNode(1);
        }
    }
}
