using System;

namespace Circuit.Elements.Gate {
    class SchmittElm : InvertingSchmittElm {
        public SchmittElm() : base() { }

        public SchmittElm(StringTokenizer st) : base(st) { }

        public override void CirDoStep() {
            double v0 = Volts[1];
            double _out;
            if (mState) {//Output is high
                if (Volts[0] > UpperTrigger)//Input voltage high enough to set output high
                {
                    mState = false;
                    _out = LogicOnLevel;
                } else {
                    _out = LogicOffLevel;
                }
            } else {//Output is low
                if (Volts[0] < LowerTrigger)//Input voltage low enough to set output low
                {
                    mState = true;
                    _out = LogicOffLevel;
                } else {
                    _out = LogicOnLevel;
                }
            }
            double maxStep = SlewRate * ControlPanel.TimeStep * 1e9;
            _out = Math.Max(Math.Min(v0 + maxStep, _out), v0 - maxStep);
            Circuit.UpdateVoltageSource(0, Nodes[1], mVoltSource, _out);
        }

        public override double GetCurrentIntoNode(int n) {
            if (n == 1) {
                return mCurrent;
            }
            return 0;
        }
    }
}
