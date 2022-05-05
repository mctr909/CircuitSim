namespace Circuit.Elements.Passive {
    class InductorElm : BaseElement {
        public Inductor Ind;

        public double Inductance { get; set; }

        public InductorElm() {
            Ind = new Inductor(mCir);
            Inductance = 0.001;
            Ind.Setup(Inductance, mCurrent, 0);
        }

        public InductorElm(double inductance, double current, int flags) {
            Ind = new Inductor(mCir);
            Inductance = inductance;
            mCurrent = current;
            Ind.Setup(Inductance, mCurrent, flags);
        }

        public override int PostCount { get { return 2; } }

        public override bool NonLinear { get { return Ind.NonLinear(); } }

        protected override void calcCurrent() {
            var voltdiff = Volts[0] - Volts[1];
            mCurrent = Ind.CalculateCurrent(voltdiff);
        }

        public override void Stamp() { Ind.Stamp(Nodes[0], Nodes[1]); }

        public override void StartIteration() {
            double voltdiff = Volts[0] - Volts[1];
            Ind.StartIteration(voltdiff);
        }

        public override void DoStep() {
            double voltdiff = Volts[0] - Volts[1];
            Ind.DoStep(voltdiff);
        }

        public override void Reset() {
            mCurrent = Volts[0] = Volts[1] = CurCount = 0;
            Ind.Reset();
        }
    }
}
