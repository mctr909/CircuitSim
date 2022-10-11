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
            var c1 = (CapacitorElm)CompElmList[0].Elm;
            ParallelCapacitance = c1.Capacitance;
            var c2 = (CapacitorElm)CompElmList[1].Elm;
            SeriesCapacitance = c2.Capacitance;
            var i1 = (InductorElm)CompElmList[2].Elm;
            Inductance = i1.Inductance;
            var r1 = (ResistorElm)CompElmList[3].Elm;
            Resistance = r1.Resistance;
            initCrystal();
        }

        public void initCrystal() {
            var c1 = (CapacitorElm)CompElmList[0].Elm;
            c1.Capacitance = ParallelCapacitance;
            var c2 = (CapacitorElm)CompElmList[1].Elm;
            c2.Capacitance = SeriesCapacitance;
            var i1 = (InductorElm)CompElmList[2].Elm;
            i1.Inductance = Inductance;
            var r1 = (ResistorElm)CompElmList[3].Elm;
            r1.Resistance = Resistance;
        }

        public override void CirSetVoltage(int n, double c) {
            base.CirSetVoltage(n, c);
            if (Volts.Length <= n) {
                return;
            }
            Volts[n] = c;
            mCurrent = GetCurrentIntoNode(1);
        }
    }
}
