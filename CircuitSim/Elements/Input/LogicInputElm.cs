using Circuit.Elements.Passive;

namespace Circuit.Elements.Input {
    class LogicInputElm : SwitchElm {
        public double mHiV;
        public double mLoV;

        public LogicInputElm() {
            mHiV = 5;
            mLoV = 0;
        }

        public LogicInputElm(StringTokenizer st) {
            try {
                mHiV = st.nextTokenDouble();
                mLoV = st.nextTokenDouble();
            } catch {
                mHiV = 5;
                mLoV = 0;
            }
        }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override int PostCount { get { return 1; } }

        public override bool AnaHasGroundConnection(int n1) { return true; }

        public override double GetCurrentIntoNode(int n) {
            return -mCurrent;
        }

        public override void CirSetCurrent(int vs, double c) { mCurrent = -c; }

        public override void AnaStamp() {
            double v = 0 != Position ? mHiV : mLoV;
            Circuit.StampVoltageSource(0, Nodes[0], mVoltSource, v);
        }
    }
}
