namespace Circuit.Elements.Input {
    class CurrentElmE : BaseElement {
        double mCurrentValue;

        public CurrentElmE() {
            mCurrentValue = 0.01;
        }

        public CurrentElmE(double current) {
            mCurrentValue = current;
        }

        public override double CirVoltageDiff { get { return CirVolts[1] - CirVolts[0]; } }

        public override double CirPower { get { return -CirVoltageDiff * mCirCurrent; } }

        /* we defer stamping current sources until we can tell if they have a current path or not */
        public void stampCurrentSource(bool broken) {
            if (broken) {
                /* no current path; stamping a current source would cause a matrix error. */
                mCir.StampResistor(CirNodes[0], CirNodes[1], 1e8);
                mCirCurrent = 0;
            } else {
                /* ok to stamp a current source */
                mCir.StampCurrentSource(CirNodes[0], CirNodes[1], mCurrentValue);
                mCirCurrent = mCurrentValue;
            }
        }
    }
}
