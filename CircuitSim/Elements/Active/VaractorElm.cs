using System;
using System.Drawing;

namespace Circuit.Elements.Active {
    class VaractorElm : DiodeElm {
        double mBaseCapacitance;
        double mCapacitance;
        double mCapCurrent;
        double mVoltSourceValue;

        // DiodeElm.lastvoltdiff = volt diff from last iteration
        // capvoltdiff = volt diff from last timestep
        double mCompResistance;
        double mCapVoltDiff;
        Point[] mPlate1;
        Point[] mPlate2;

        public VaractorElm(Point pos) : base(pos) {
            mBaseCapacitance = 4e-12;
        }

        public VaractorElm(Point a, Point b, int f, StringTokenizer st) : base(a, b, f, st) {
            mCapVoltDiff = double.Parse(st.nextToken());
            mBaseCapacitance = double.Parse(st.nextToken());
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.VARACTOR; } }

        public override DUMP_ID Shortcut { get { return DUMP_ID.VARACTOR; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override int InternalNodeCount { get { return 1; } }

        public override void SetCurrent(int x, double c) { mCapCurrent = c; }

        protected override string dump() {
            return mCapVoltDiff + " " + mBaseCapacitance;
        }

        protected override void calculateCurrent() {
            base.calculateCurrent();
            mCurrent += mCapCurrent;
        }

        public override void SetNodeVoltage(int n, double c) {
            base.SetNodeVoltage(n, c);
            mCapVoltDiff = Volts[0] - Volts[1];
        }

        public override void Stamp() {
            base.Stamp();
            mCir.StampVoltageSource(Nodes[0], Nodes[2], mVoltSource);
            mCir.StampNonLinear(Nodes[2]);
        }

        public override void DoStep() {
            base.DoStep();
            mCir.StampResistor(Nodes[2], Nodes[1], mCompResistance);
            mCir.UpdateVoltageSource(Nodes[0], Nodes[2], mVoltSource, mVoltSourceValue);
        }

        public override void StartIteration() {
            base.StartIteration();
            // capacitor companion model using trapezoidal approximation
            // (Thevenin equivalent) consists of a voltage source in
            // series with a resistor
            double c0 = mBaseCapacitance;
            if (0 < mCapVoltDiff) {
                mCapacitance = c0;
            } else {
                mCapacitance = c0 / Math.Pow(1 - mCapVoltDiff / mModel.fwdrop, 0.5);
            }
            mCompResistance = ControlPanel.TimeStep / (2 * mCapacitance);
            mVoltSourceValue = -mCapVoltDiff - mCapCurrent * mCompResistance;
        }

        public override void SetPoints() {
            base.SetPoints();
            double platef = 0.6;
            var pa = new Point[2];
            interpLeadAB(ref pa[0], ref pa[1], 0, HS);
            interpLeadAB(ref mCathode[0], ref mCathode[1], platef, HS);
            var arrowPoint = new Point();
            interpLead(ref arrowPoint, platef);
            mPoly = new Point[] { pa[0], pa[1], arrowPoint };
            // calc plates
            mPlate1 = new Point[2];
            mPlate2 = new Point[2];
            interpLeadAB(ref mPlate1[0], ref mPlate1[1], platef, HS);
            interpLeadAB(ref mPlate2[0], ref mPlate2[1], 1, HS);
        }

        public override void Reset() {
            base.Reset();
            mCapVoltDiff = 0;
        }

        public override void Draw(CustomGraphics g) {
            // draw leads and diode arrow
            drawDiode(g);

            // draw first plate
            drawVoltage(0, mPlate1[0], mPlate1[1]);

            // draw second plate
            drawVoltage(1, mPlate2[0], mPlate2[1]);

            doDots();
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            base.GetInfo(arr);
            arr[0] = "varactor";
            arr[5] = "C = " + Utils.UnitText(mCapacitance, "F");
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 1) {
                return new ElementInfo("静電容量(F) @ 0V", mBaseCapacitance, 10, 1000);
            }
            return base.GetElementInfo(n);
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 1) {
                mBaseCapacitance = ei.Value;
                return;
            }
            base.SetElementValue(n, ei);
        }
    }
}
