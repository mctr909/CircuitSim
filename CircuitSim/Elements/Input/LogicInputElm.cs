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

        public override bool HasGroundConnection(int n1) { return true; }

        public override double GetCurrentIntoNode(int n) {
            return -mCurrent;
        }

        public override void SetCurrent(int vs, double c) { mCurrent = -c; }

        public override void Stamp() {
            double v = 0 != Position ? mHiV : mLoV;
            mCir.StampVoltageSource(0, Nodes[0], mVoltSource, v);
        }
    }
}
