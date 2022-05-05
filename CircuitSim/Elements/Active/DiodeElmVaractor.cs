﻿using System;

namespace Circuit.Elements.Active {
    class DiodeElmVaractor : DiodeElm {
        public double mBaseCapacitance;
        public double mCapacitance;
        double mCapCurrent;
        double mVoltSourceValue;

        // DiodeElm.lastvoltdiff = volt diff from last iteration
        // capvoltdiff = volt diff from last timestep
        double mCompResistance;
        public double mCapVoltDiff;

        public DiodeElmVaractor() : base() {
            mBaseCapacitance = 4e-12;
        }

        public DiodeElmVaractor(StringTokenizer st) : base(st) {
            mModelName = st.nextToken();
            mCapVoltDiff = double.Parse(st.nextToken());
            mBaseCapacitance = double.Parse(st.nextToken());
        }

        public override int VoltageSourceCount { get { return 1; } }

        public override int InternalNodeCount { get { return 1; } }

        public override void CirSetCurrent(int x, double c) { mCapCurrent = c; }

        protected override void cirCalcCurrent() {
            base.cirCalcCurrent();
            mCurrent += mCapCurrent;
        }

        public override void CirSetNodeVoltage(int n, double c) {
            base.CirSetNodeVoltage(n, c);
            mCapVoltDiff = Volts[0] - Volts[1];
        }

        public override void AnaStamp() {
            base.AnaStamp();
            mCir.StampVoltageSource(Nodes[0], Nodes[2], mVoltSource);
            mCir.StampNonLinear(Nodes[2]);
        }

        public override void CirDoStep() {
            base.CirDoStep();
            mCir.StampResistor(Nodes[2], Nodes[1], mCompResistance);
            mCir.UpdateVoltageSource(Nodes[0], Nodes[2], mVoltSource, mVoltSourceValue);
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

        public override void Reset() {
            base.Reset();
            mCapVoltDiff = 0;
        }
    }
}
