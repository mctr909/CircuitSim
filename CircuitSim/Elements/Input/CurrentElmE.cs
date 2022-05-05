namespace Circuit.Elements.Input {
    class CurrentElmE : BaseElement {
        double mCurrentValue;

        public CurrentElmE() {
            mCurrentValue = 0.01;
        }

        public CurrentElmE(double current) {
            mCurrentValue = current;
        }

        public override int PostCount { get { return 2; } }

        public override double VoltageDiff { get { return Volts[1] - Volts[0]; } }

        public override double Power { get { return -VoltageDiff * mCurrent; } }

        /* we defer stamping current sources until we can tell if they have a current path or not */
        public void stampCurrentSource(bool broken) {
            if (broken) {
                /* no current path; stamping a current source would cause a matrix error. */
                mCir.StampResistor(Nodes[0], Nodes[1], 1e8);
                mCurrent = 0;
            } else {
                /* ok to stamp a current source */
                mCir.StampCurrentSource(Nodes[0], Nodes[1], mCurrentValue);
                mCurrent = mCurrentValue;
            }
        }
    }
}
