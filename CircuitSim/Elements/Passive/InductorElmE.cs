namespace Circuit.Elements.Passive {
    class InductorElmE : BaseElement {
        public Inductor Ind;

        public double Inductance { get; set; }

        public InductorElmE() {
            Ind = new Inductor(mCir);
            Inductance = 0.001;
            Ind.Setup(Inductance, mCirCurrent, 0);
        }

        public InductorElmE(double inductance, double current, int flags) {
            Ind = new Inductor(mCir);
            Inductance = inductance;
            mCirCurrent = current;
            Ind.Setup(Inductance, mCirCurrent, flags);
        }

        public override bool CirNonLinear { get { return Ind.NonLinear(); } }

        protected override void cirCalculateCurrent() {
            var voltdiff = CirVolts[0] - CirVolts[1];
            mCirCurrent = Ind.CalculateCurrent(voltdiff);
        }

        public override void CirStamp() { Ind.Stamp(CirNodes[0], CirNodes[1]); }

        public override void CirStartIteration() {
            double voltdiff = CirVolts[0] - CirVolts[1];
            Ind.StartIteration(voltdiff);
        }

        public override void CirDoStep() {
            double voltdiff = CirVolts[0] - CirVolts[1];
            Ind.DoStep(voltdiff);
        }

        public override void CirReset() {
            mCirCurrent = CirVolts[0] = CirVolts[1] = mCirCurCount = 0;
            Ind.Reset();
        }
    }
}
