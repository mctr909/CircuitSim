using System;

namespace Circuit.Elements.Gate {
    class InvertingSchmittElm : BaseElement {
        public double SlewRate; // V/ns
        public double LowerTrigger;
        public double UpperTrigger;
        public double LogicOnLevel;
        public double LogicOffLevel;

        protected bool mState;

        public InvertingSchmittElm() : base() {
            SlewRate = 0.5;
            mState = false;
            LowerTrigger = 1.66;
            UpperTrigger = 3.33;
            LogicOnLevel = 5;
            LogicOffLevel = 0;
        }

        public InvertingSchmittElm(StringTokenizer st) : base() {
            try {
                SlewRate = st.nextTokenDouble();
                LowerTrigger = st.nextTokenDouble();
                UpperTrigger = st.nextTokenDouble();
                LogicOnLevel = st.nextTokenDouble();
                LogicOffLevel = st.nextTokenDouble();
            } catch {
                SlewRate = 0.5;
                LowerTrigger = 1.66;
                UpperTrigger = 3.33;
                LogicOnLevel = 5;
                LogicOffLevel = 0;
            }
        }

        public override int PostCount { get { return 2; } }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override int VoltageSourceCount { get { return 1; } }

        // there is no current path through the InvertingSchmitt input, but there
        // is an indirect path through the output to ground.
        public override bool GetConnection(int n1, int n2) { return false; }

        public override void AnaStamp() {
            Circuit.StampVoltageSource(0, Nodes[1], mVoltSource);
        }

        public override void CirDoStep() {
            double v0 = Volts[1];
            double _out;
            if (mState) {//Output is high
                if (Volts[0] > UpperTrigger)//Input voltage high enough to set output low
                {
                    mState = false;
                    _out = LogicOffLevel;
                } else {
                    _out = LogicOnLevel;
                }
            } else {//Output is low
                if (Volts[0] < LowerTrigger)//Input voltage low enough to set output high
                {
                    mState = true;
                    _out = LogicOnLevel;
                } else {
                    _out = LogicOffLevel;
                }
            }
            double maxStep = SlewRate * ControlPanel.TimeStep * 1e9;
            _out = Math.Max(Math.Min(v0 + maxStep, _out), v0 - maxStep);
            Circuit.UpdateVoltageSource(0, Nodes[1], mVoltSource, _out);
        }

        public override bool AnaHasGroundConnection(int n1) { return n1 == 1; }

        public override double GetCurrentIntoNode(int n) {
            if (n == 1) {
                return mCurrent;
            }
            return 0;
        }
    }
}
