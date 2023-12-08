using System;

namespace Circuit.Elements.Active {
    class ElmDiodeVaractor : ElmDiode {
        public double mBaseCapacitance;
        public double mCapacitance;
        double mCapCurrent;
        double mVoltSourceValue;

        // DiodeElm.lastvoltdiff = volt diff from last iteration
        // capvoltdiff = volt diff from last timestep
        double mCompResistance;
        public double mCapVoltDiff;

        public ElmDiodeVaractor() : base() {
            mBaseCapacitance = 4e-12;
        }

        public ElmDiodeVaractor(StringTokenizer st) : base(st) {
            st.nextToken(out mModelName, mModelName);
            mCapVoltDiff = st.nextTokenDouble();
            mBaseCapacitance = st.nextTokenDouble();
        }

        public override int VoltageSourceCount { get { return 1; } }

        public override int InternalNodeCount { get { return 1; } }

        public override void Reset() {
            base.Reset();
            mCapVoltDiff = 0;
        }

        public override void Stamp() {
            base.Stamp();
            var n0 = Nodes[0] - 1;
            var n1 = Nodes[2] - 1;
            int vn = Circuit.Nodes.Count + mVoltSource - 1;
            Circuit.Matrix[vn, n0] -= 1;
            Circuit.Matrix[vn, n1] += 1;
            Circuit.Matrix[n0, vn] += 1;
            Circuit.Matrix[n1, vn] -= 1;
            Circuit.RowInfo[vn].RightChanges = true;
            Circuit.RowInfo[n1].LeftChanges = true;
        }

        public override void DoIteration() {
            base.DoIteration();
            var g = 1.0 / mCompResistance;
            var n0 = Nodes[2] - 1;
            var n1 = Nodes[1] - 1;
            var vn = Circuit.Nodes.Count + mVoltSource - 1;
            Circuit.Matrix[n0, n0] += g;
            Circuit.Matrix[n1, n1] += g;
            Circuit.Matrix[n0, n1] -= g;
            Circuit.Matrix[n1, n0] -= g;
            Circuit.RightSide[vn] += mVoltSourceValue;
        }

        public override void PrepareIteration() {
            base.PrepareIteration();
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

        public override void SetVoltage(int n, double c) {
            base.SetVoltage(n, c);
            mCapVoltDiff = Volts[0] - Volts[1];
            Current += mCapCurrent;
        }

        public override void SetCurrent(int x, double c) { mCapCurrent = c; }
    }
}
