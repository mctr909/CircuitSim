namespace Circuit.Elements.Passive {
    class InductorElm : BaseElement {
        public Inductor Ind;

        public double Inductance { get; set; }

        public InductorElm() {
            Ind = new Inductor(mCir);
            Inductance = 0.001;
            Ind.Setup(Inductance, mCurrent);
        }

        public InductorElm(double inductance, double current, int flags) {
            Ind = new Inductor(mCir);
            Inductance = inductance;
            mCurrent = current;
            Ind.Setup(Inductance, mCurrent);
        }

        public override int PostCount { get { return 2; } }

        public override bool NonLinear { get { return Ind.NonLinear(); } }

        public override void Reset() {
            mCurrent = Volts[0] = Volts[1] = CurCount = 0;
            Ind.Reset();
        }

        public override void AnaStamp() { Ind.Stamp(Nodes[0], Nodes[1]); }

        public override void CirDoStep() {
            double voltdiff = Volts[0] - Volts[1];
            Ind.DoStep(voltdiff);
        }

        public override void CirStartIteration() {
            double voltdiff = Volts[0] - Volts[1];
            Ind.StartIteration(voltdiff);
        }

        protected override void cirCalcCurrent() {
            var voltdiff = Volts[0] - Volts[1];
            mCurrent = Ind.CalculateCurrent(voltdiff);
        }
    }
}
