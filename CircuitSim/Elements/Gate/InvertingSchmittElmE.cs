using System;

namespace Circuit.Elements.Gate {
    class InvertingSchmittElmE : BaseElement {
        public double SlewRate; // V/ns
        public double LowerTrigger;
        public double UpperTrigger;
        public double LogicOnLevel;
        public double LogicOffLevel;

        protected bool mState;

        public InvertingSchmittElmE() : base() {
            SlewRate = 0.5;
            mState = false;
            LowerTrigger = 1.66;
            UpperTrigger = 3.33;
            LogicOnLevel = 5;
            LogicOffLevel = 0;
        }

        public InvertingSchmittElmE(StringTokenizer st) : base() {
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

        public override double CirVoltageDiff { get { return CirVolts[0]; } }

        public override int CirVoltageSourceCount { get { return 1; } }

        public override void CirStamp() {
            mCir.StampVoltageSource(0, CirNodes[1], mCirVoltSource);
        }

        public override void CirDoStep() {
            double v0 = CirVolts[1];
            double _out;
            if (mState) {//Output is high
                if (CirVolts[0] > UpperTrigger)//Input voltage high enough to set output low
                {
                    mState = false;
                    _out = LogicOffLevel;
                } else {
                    _out = LogicOnLevel;
                }
            } else {//Output is low
                if (CirVolts[0] < LowerTrigger)//Input voltage low enough to set output high
                {
                    mState = true;
                    _out = LogicOnLevel;
                } else {
                    _out = LogicOffLevel;
                }
            }
            double maxStep = SlewRate * ControlPanel.TimeStep * 1e9;
            _out = Math.Max(Math.Min(v0 + maxStep, _out), v0 - maxStep);
            mCir.UpdateVoltageSource(0, CirNodes[1], mCirVoltSource, _out);
        }

        public override bool CirHasGroundConnection(int n1) { return n1 == 1; }

        public override double CirGetCurrentIntoNode(int n) {
            if (n == 1) {
                return mCirCurrent;
            }
            return 0;
        }
    }
}
