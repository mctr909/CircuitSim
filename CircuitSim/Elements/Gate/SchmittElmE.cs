using System;

namespace Circuit.Elements.Gate {
    class SchmittElmE : InvertingSchmittElmE {
        public SchmittElmE() : base() { }

        public SchmittElmE(StringTokenizer st) : base(st) { }

        public override void CirDoStep() {
            double v0 = CirVolts[1];
            double _out;
            if (mState) {//Output is high
                if (CirVolts[0] > UpperTrigger)//Input voltage high enough to set output high
                {
                    mState = false;
                    _out = LogicOnLevel;
                } else {
                    _out = LogicOffLevel;
                }
            } else {//Output is low
                if (CirVolts[0] < LowerTrigger)//Input voltage low enough to set output low
                {
                    mState = true;
                    _out = LogicOffLevel;
                } else {
                    _out = LogicOnLevel;
                }
            }
            double maxStep = SlewRate * ControlPanel.TimeStep * 1e9;
            _out = Math.Max(Math.Min(v0 + maxStep, _out), v0 - maxStep);
            mCir.UpdateVoltageSource(0, CirNodes[1], mCirVoltSource, _out);
        }

        public override double CirGetCurrentIntoNode(int n) {
            if (n == 1) {
                return mCirCurrent;
            }
            return 0;
        }
    }
}
