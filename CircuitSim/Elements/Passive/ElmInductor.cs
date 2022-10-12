﻿namespace Circuit.Elements.Passive {
    class ElmInductor : BaseElement {
        double mCompResistance;
        double mCurSourceValue;

        public double Inductance = 0.001;

        public ElmInductor() : base() { }

        public ElmInductor(double inductance, double c) : base() {
            Inductance = inductance;
            mCurrent = c;
        }

        public override int PostCount { get { return 2; } }

        public override bool NonLinear { get { return false; } }

        public void Setup(double ic, double cr) {
            Inductance = ic;
            mCurrent = cr;
        }

        public override void Reset() {
            mCurrent = Volts[0] = Volts[1] = CurCount = mCurSourceValue = 0;
        }

        public override void AnaStamp() {
            mCompResistance = 2 * Inductance / ControlPanel.TimeStep;
            Circuit.StampResistor(Nodes[0], Nodes[1], mCompResistance);
            Circuit.StampRightSide(Nodes[0]);
            Circuit.StampRightSide(Nodes[1]);
        }

        public override void CirPrepareIteration() {
            double voltdiff = Volts[0] - Volts[1];
            mCurSourceValue = voltdiff / mCompResistance + mCurrent;
        }

        public override void CirDoIteration() {
            var r = Circuit.mRowInfo[Nodes[0] - 1].MapRow;
            Circuit.mRightSide[r] -= mCurSourceValue;
            r = Circuit.mRowInfo[Nodes[1] - 1].MapRow;
            Circuit.mRightSide[r] += mCurSourceValue;
        }

        public override void CirSetVoltage(int n, double c) {
            Volts[n] = c;
            var voltdiff = Volts[0] - Volts[1];
            mCurrent = voltdiff / mCompResistance + mCurSourceValue;
        }
    }
}
