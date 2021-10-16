using System;
using System.Drawing;

namespace Circuit.Elements.Active {
    class VaractorElm : DiodeElm {
        double baseCapacitance;
        double capacitance;
        double capCurrent;
        double voltSourceValue;

        // DiodeElm.lastvoltdiff = volt diff from last iteration
        // capvoltdiff = volt diff from last timestep
        double compResistance;
        double capvoltdiff;
        Point[] plate1;
        Point[] plate2;

        public VaractorElm(Point pos) : base(pos) {
            baseCapacitance = 4e-12;
        }

        public VaractorElm(Point a, Point b, int f, StringTokenizer st) : base(a, b, f, st) {
            capvoltdiff = double.Parse(st.nextToken());
            baseCapacitance = double.Parse(st.nextToken());
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.VARACTOR; } }

        public override DUMP_ID Shortcut { get { return DUMP_ID.VARACTOR; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override int InternalNodeCount { get { return 1; } }

        public override void SetCurrent(int x, double c) { capCurrent = c; }

        protected override string dump() {
            return capvoltdiff + " " + baseCapacitance;
        }

        protected override void calculateCurrent() {
            base.calculateCurrent();
            mCurrent += capCurrent;
        }

        public override void GetInfo(string[] arr) {
            base.GetInfo(arr);
            arr[0] = "varactor";
            arr[5] = "C = " + Utils.UnitText(capacitance, "F");
        }

        public override void SetNodeVoltage(int n, double c) {
            base.SetNodeVoltage(n, c);
            capvoltdiff = Volts[0] - Volts[1];
        }

        public override void SetPoints() {
            base.SetPoints();
            double platef = 0.6;
            var pa = new Point[2];
            interpLeadAB(ref pa[0], ref pa[1], 0, hs);
            interpLeadAB(ref mCathode[0], ref mCathode[1], platef, hs);
            var arrowPoint = new Point();
            interpLead(ref arrowPoint, platef);
            poly = new Point[] { pa[0], pa[1], arrowPoint };
            // calc plates
            plate1 = new Point[2];
            plate2 = new Point[2];
            interpLeadAB(ref plate1[0], ref plate1[1], platef, hs);
            interpLeadAB(ref plate2[0], ref plate2[1], 1, hs);
        }

        public override void Draw(CustomGraphics g) {
            // draw leads and diode arrow
            drawDiode(g);

            // draw first plate
            drawVoltage(g, 0, plate1[0], plate1[1]);

            // draw second plate
            drawVoltage(g, 1, plate2[0], plate2[1]);

            doDots(g);
            drawPosts(g);
        }

        public override void Stamp() {
            base.Stamp();
            mCir.StampVoltageSource(Nodes[0], Nodes[2], mVoltSource);
            mCir.StampNonLinear(Nodes[2]);
        }

        public override void Reset() {
            base.Reset();
            capvoltdiff = 0;
        }

        public override void StartIteration() {
            base.StartIteration();
            // capacitor companion model using trapezoidal approximation
            // (Thevenin equivalent) consists of a voltage source in
            // series with a resistor
            double c0 = baseCapacitance;
            if (0 < capvoltdiff) {
                capacitance = c0;
            } else {
                capacitance = c0 / Math.Pow(1 - capvoltdiff / model.fwdrop, 0.5);
            }
            compResistance = ControlPanel.TimeStep / (2 * capacitance);
            voltSourceValue = -capvoltdiff - capCurrent * compResistance;
        }

        public override void DoStep() {
            base.DoStep();
            mCir.StampResistor(Nodes[2], Nodes[1], compResistance);
            mCir.UpdateVoltageSource(Nodes[0], Nodes[2], mVoltSource, voltSourceValue);
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 1) {
                return new ElementInfo("Capacitance @ 0V (F)", baseCapacitance, 10, 1000);
            }
            return base.GetElementInfo(n);
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 1) {
                baseCapacitance = ei.Value;
                return;
            }
            base.SetElementValue(n, ei);
        }
    }
}
