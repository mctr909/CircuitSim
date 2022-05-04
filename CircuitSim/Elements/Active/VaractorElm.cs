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
            ReferenceName = "Vc";
            mBaseCapacitance = 4e-12;
        }

        public VaractorElm(Point a, Point b, int f, StringTokenizer st) : base(a, b, f, st) {
            mCapVoltDiff = double.Parse(st.nextToken());
            mBaseCapacitance = double.Parse(st.nextToken());
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.VARACTOR; } }

        public override DUMP_ID Shortcut { get { return DUMP_ID.VARACTOR; } }

        public override int CirVoltageSourceCount { get { return 1; } }

        public override int CirInternalNodeCount { get { return 1; } }

        public override void CirSetCurrent(int x, double c) { mCapCurrent = c; }

        protected override string dump() {
            return base.dump() + " " + mCapVoltDiff + " " + mBaseCapacitance;
        }

        protected override void cirCalculateCurrent() {
            base.cirCalculateCurrent();
            mCirCurrent += mCapCurrent;
        }

        public override void CirSetNodeVoltage(int n, double c) {
            base.CirSetNodeVoltage(n, c);
            mCapVoltDiff = CirVolts[0] - CirVolts[1];
        }

        public override void CirStamp() {
            base.CirStamp();
            mCir.StampVoltageSource(CirNodes[0], CirNodes[2], mCirVoltSource);
            mCir.StampNonLinear(CirNodes[2]);
        }

        public override void CirDoStep() {
            base.CirDoStep();
            mCir.StampResistor(CirNodes[2], CirNodes[1], mCompResistance);
            mCir.UpdateVoltageSource(CirNodes[0], CirNodes[2], mCirVoltSource, mVoltSourceValue);
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
            setTextPos();
        }

        public override void CirReset() {
            base.CirReset();
            mCapVoltDiff = 0;
        }

        public override void Draw(CustomGraphics g) {
            // draw leads and diode arrow
            drawDiode(g);

            // draw first plate
            drawLead(mPlate1[0], mPlate1[1]);

            // draw second plate
            drawLead(mPlate2[0], mPlate2[1]);

            doDots();
            drawPosts();
            drawName();
        }

        public override void GetInfo(string[] arr) {
            base.GetInfo(arr);
            arr[0] = "varactor";
            arr[5] = "C = " + Utils.UnitText(mCapacitance, "F");
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 2) {
                return new ElementInfo("静電容量(F) @ 0V", mBaseCapacitance, 10, 1000);
            }
            return base.GetElementInfo(n);
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 2) {
                mBaseCapacitance = ei.Value;
                return;
            }
            base.SetElementValue(n, ei);
        }
    }
}
