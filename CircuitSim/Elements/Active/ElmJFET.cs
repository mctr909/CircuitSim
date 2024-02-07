namespace Circuit.Elements.Active {
    class ElmJFET : ElmFET {
        double mGateCurrent;

        public ElmJFET(bool isNch, double vth, double beta) : base(isNch, false, vth, beta) { }

        public override double GetCurrentIntoNode(int n) {
            if (n == 0) {
                return -mGateCurrent;
            }
            if (n == 1) {
                return mGateCurrent + Current;
            }
            return -Current;
        }

        public override void SetCurrent(int n, double c) {
            mGateCurrent = Nch * CalculateDiodeCurrent(Nch * (Volts[IdxG] - Volts[IdxS]));
        }
    }
}
