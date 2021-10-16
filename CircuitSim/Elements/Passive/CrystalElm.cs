using System.Drawing;

using Circuit.Elements.Custom;

namespace Circuit.Elements.Passive {
    class CrystalElm : CompositeElm {
        private static int[] modelExternalNodes = { 1, 2 };
        private static string modelString
            = ELEMENTS.CAPACITOR + " 1 2\r"
            + ELEMENTS.CAPACITOR + " 1 3\r"
            + ELEMENTS.INDUCTOR + " 3 4\r"
            + ELEMENTS.RESISTOR + " 4 2";

        double seriesCapacitance;
        double parallelCapacitance;
        double inductance;
        double resistance;
        Point[] plate1;
        Point[] plate2;
        Point[] sandwichPoints;

        public override bool CanViewInScope { get { return true; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.CRYSTAL; } }

        public CrystalElm(Point pos) : base(pos, modelString, modelExternalNodes) {
            parallelCapacitance = 28.7e-12;
            seriesCapacitance = 0.1e-12;
            inductance = 2.5e-3;
            resistance = 6.4;
            initCrystal();
        }

        public CrystalElm(Point a, Point b, int f, StringTokenizer st) : base(a, b, f, st, modelString, modelExternalNodes) {
            var c1 = (CapacitorElm)compElmList[0];
            parallelCapacitance = c1.Capacitance;
            var c2 = (CapacitorElm)compElmList[1];
            seriesCapacitance = c2.Capacitance;
            var i1 = (InductorElm)compElmList[2];
            inductance = i1.Inductance;
            var r1 = (ResistorElm)compElmList[3];
            resistance = r1.Resistance;
            initCrystal();
        }

        private void initCrystal() {
            var c1 = (CapacitorElm)compElmList[0];
            c1.Capacitance = parallelCapacitance;
            var c2 = (CapacitorElm)compElmList[1];
            c2.Capacitance = seriesCapacitance;
            var i1 = (InductorElm)compElmList[2];
            i1.Inductance = inductance;
            var r1 = (ResistorElm)compElmList[3];
            r1.Resistance = resistance;
        }

        protected override void calculateCurrent() {
            mCurrent = GetCurrentIntoNode(1);
        }

        public override void SetPoints() {
            base.SetPoints();
            double f = (mLen / 2 - 8) / mLen;

            // calc leads
            setLead1(f);
            setLead2(1 - f);

            // calc plates
            plate1 = new Point[2];
            plate2 = new Point[2];
            interpPointAB(ref plate1[0], ref plate1[1], f, 6);
            interpPointAB(ref plate2[0], ref plate2[1], 1 - f, 6);

            double f2 = (mLen / 2 - 4) / mLen;
            sandwichPoints = new Point[4];
            interpPointAB(ref sandwichPoints[0], ref sandwichPoints[1], f2, 8);
            interpPointAB(ref sandwichPoints[3], ref sandwichPoints[2], 1 - f2, 8);

            // need to do this explicitly for CompositeElms
            setPost(0, mPoint1);
            setPost(1, mPoint2);
        }

        public override void Draw(CustomGraphics g) {
            int hs = 12;
            setBbox(mPoint1, mPoint2, hs);

            // draw first lead and plate
            drawVoltage(g, 0, mPoint1, mLead1);
            g.DrawThickLine(plate1[0], plate1[1]);

            // draw second lead and plate
            drawVoltage(g, 1, mPoint2, mLead2);
            g.DrawThickLine(plate2[0], plate2[1]);

            g.ThickLineColor = getVoltageColor(0.5 * (Volts[0] + Volts[1]));
            for (int i = 0; i != 4; i++) {
                g.DrawThickLine(sandwichPoints[i], sandwichPoints[(i + 1) % 4]);
            }

            updateDotCount();
            if (CirSim.Sim.DragElm != this) {
                drawDots(g, mPoint1, mLead1, mCurCount);
                drawDots(g, mPoint2, mLead2, -mCurCount);
            }
            drawPosts(g);
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "crystal";
            getBasicInfo(arr);
            //	    arr[3] = "C = " + getUnitText(capacitance, "F");
            //	    arr[4] = "P = " + getUnitText(getPower(), "W");
            //double v = getVoltageDiff();
            //arr[4] = "U = " + getUnitText(.5*capacitance*v*v, "J");
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo(ElementInfo.MakeLink("crystal.html", "Parallel Capacitance"), parallelCapacitance);
            }
            if (n == 1) {
                return new ElementInfo("Series Capacitance (F)", seriesCapacitance);
            }
            if (n == 2) {
                return new ElementInfo("Inductance (H)", inductance, 0, 0);
            }
            if (n == 3) {
                return new ElementInfo("Resistance (Ω)", resistance, 0, 0);
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0 && 0 < ei.Value) {
                parallelCapacitance = ei.Value;
            }
            if (n == 1 && 0 < ei.Value) {
                seriesCapacitance = ei.Value;
            }
            if (n == 2 && 0 < ei.Value) {
                inductance = ei.Value;
            }
            if (n == 3 && 0 < ei.Value) {
                resistance = ei.Value;
            }
            initCrystal();
        }
    }
}
