using System;

namespace Circuit.Elements.Active {
    class VaractorElmE : DiodeElmE {
        public double mBaseCapacitance;
        public double mCapacitance;
        double mCapCurrent;
        double mVoltSourceValue;

        // DiodeElm.lastvoltdiff = volt diff from last iteration
        // capvoltdiff = volt diff from last timestep
        double mCompResistance;
        public double mCapVoltDiff;

        public VaractorElmE() : base() {
            mBaseCapacitance = 4e-12;
        }

        public VaractorElmE(StringTokenizer st) : base(st) {
            mModelName = st.nextToken();
            mCapVoltDiff = double.Parse(st.nextToken());
            mBaseCapacitance = double.Parse(st.nextToken());
        }

        public override int CirVoltageSourceCount { get { return 1; } }

        public override int CirInternalNodeCount { get { return 1; } }

        public override void CirSetCurrent(int x, double c) { mCapCurrent = c; }

        protected override void cirCalculateCurrent() {
            base.cirCalculateCurrent();
            mCirCurrent += mCapCurrent;
        }

        public override void CirSetNodeVoltage(int n, double c) {
            base.CirSetNodeVoltage(n, c);
            mCapVoltDiff = CirVolts[0] - CirVolts[1];
        }

        public override void CirStamp() {
            base.CirStamp();
            mCir.StampVoltageSource(CirNodes[0], CirNodes[2], mCirVoltSource);
            mCir.StampNonLinear(CirNodes[2]);
        }

        public override void CirDoStep() {
            base.CirDoStep();
            mCir.StampResistor(CirNodes[2], CirNodes[1], mCompResistance);
            mCir.UpdateVoltageSource(CirNodes[0], CirNodes[2], mCirVoltSource, mVoltSourceValue);
        }

        public override void CirStartIteration() {
            base.CirStartIteration();
            // capacitor companion model using trapezoidal approximation
            // (Thevenin equivalent) consists of a voltage source in
            // series with a resistor
            double c0 = mBaseCapacitance;
            if (0 < mCapVoltDiff) {
                mCapacitance = c0;
            } else {
                mCapacitance = c0 / Math.Pow(1 - mCapVoltDiff / mModel.FwDrop, 0.5);
            }
            mCompResistance = ControlPanel.TimeStep / (2 * mCapacitance);
            mVoltSourceValue = -mCapVoltDiff - mCapCurrent * mCompResistance;
        }

        public override void CirReset() {
            base.CirReset();
            mCapVoltDiff = 0;
        }
    }
}
