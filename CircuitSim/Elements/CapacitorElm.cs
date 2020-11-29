using System.Windows.Forms;
using System.Drawing;

namespace Circuit.Elements {
    class CapacitorElm : CircuitElm {
        public double capacitance { get; private set; }
        double compResistance;
        double voltdiff;
        double curSourceValue;

        Point[] plate1;
        Point[] plate2;

        /* used for PolarCapacitorElm */
        Point[] platePoints;

        public static readonly int FLAG_BACK_EULER = 2;

        public CapacitorElm(int xx, int yy) : base(xx, yy) {
            capacitance = 1e-5;
        }

        public CapacitorElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            capacitance = st.nextTokenDouble();
            voltdiff = st.nextTokenDouble();
        }

        public bool isTrapezoidal() { return (flags & FLAG_BACK_EULER) == 0; }

        public double getCapacitance() { return capacitance; }

        public void setCapacitance(double c) { capacitance = c; }

        public override void setNodeVoltage(int n, double c) {
            base.setNodeVoltage(n, c);
            voltdiff = volts[0] - volts[1];
        }

        public override void reset() {
            base.reset();
            current = curcount = curSourceValue = 0;
            /* put small charge on caps when reset to start oscillators */
            voltdiff = 1e-3;
        }

        public void shorted() {
            base.reset();
            voltdiff = current = curcount = curSourceValue = 0;
        }

        public override DUMP_ID getDumpType() { return DUMP_ID.CAPACITOR; }

        public override string dump() {
            return base.dump() + " " + capacitance + " " + voltdiff;
        }

        public override void setPoints() {
            base.setPoints();
            double f = (dn / 2 - 4) / dn;
            /* calc leads */
            lead1 = interpPoint(point1, point2, f);
            lead2 = interpPoint(point1, point2, 1 - f);
            /* calc plates */
            plate1 = newPointArray(2);
            plate2 = newPointArray(2);
            interpPoint(point1, point2, ref plate1[0], ref plate1[1], f, 10);
            interpPoint(point1, point2, ref plate2[0], ref plate2[1], 1 - f, 10);
        }

        public override void draw(Graphics g) {
            int hs = 8;
            setBbox(point1, point2, hs);

            /* draw first lead and plate */
            PEN_THICK_LINE.Color = getVoltageColor(volts[0]);
            drawThickLine(g, point1, lead1);
            drawThickLine(g, plate1[0], plate1[1]);
            /* draw second lead and plate */
            PEN_THICK_LINE.Color = getVoltageColor(volts[1]);
            drawThickLine(g, point2, lead2);
            drawThickLine(g, plate2[0], plate2[1]);

            if (platePoints == null) {
                drawThickLine(g, plate2[0], plate2[1]);
            } else {
                for (int i = 0; i != 7; i++) {
                    drawThickLine(g, platePoints[i], platePoints[i + 1]);
                }
            }

            updateDotCount();
            if (sim.dragElm != this) {
                drawDots(g, point1, lead1, curcount);
                drawDots(g, point2, lead2, -curcount);
            }
            drawPosts(g);
            if (sim.chkShowValuesCheckItem.Checked) {
                var s = getShortUnitText(capacitance, "F");
                drawValues(g, s, hs);
            }
        }

        public override void stamp() {
            if (sim.dcAnalysisFlag) {
                /* when finding DC operating point, replace cap with a 100M resistor */
                cir.stampResistor(nodes[0], nodes[1], 1e8);
                curSourceValue = 0;
                return;
            }

            /* capacitor companion model using trapezoidal approximation
             * (Norton equivalent) consists of a current source in
             * parallel with a resistor.  Trapezoidal is more accurate
             * than backward euler but can cause oscillatory behavior
             * if RC is small relative to the timestep. */
            if (isTrapezoidal()) {
                compResistance = sim.timeStep / (2 * capacitance);
            } else {
                compResistance = sim.timeStep / capacitance;
            }
            cir.stampResistor(nodes[0], nodes[1], compResistance);
            cir.stampRightSide(nodes[0]);
            cir.stampRightSide(nodes[1]);
        }

        public override void startIteration() {
            if (isTrapezoidal()) {
                curSourceValue = -voltdiff / compResistance - current;
            } else {
                curSourceValue = -voltdiff / compResistance;
            }
        }

        public override void calculateCurrent() {
            double voltdiff = volts[0] - volts[1];
            if (sim.dcAnalysisFlag) {
                current = voltdiff / 1e8;
                return;
            }
            /* we check compResistance because this might get called
             * before stamp(), which sets compResistance, causing
             * infinite current */
            if (compResistance > 0) {
                current = voltdiff / compResistance + curSourceValue;
            }
        }

        public override void doStep() {
            if (sim.dcAnalysisFlag) {
                return;
            }
            cir.stampCurrentSource(nodes[0], nodes[1], curSourceValue);
        }

        public override void getInfo(string[] arr) {
            arr[0] = "capacitor";
            getBasicInfo(arr);
            arr[3] = "C = " + getUnitText(capacitance, "F");
            arr[4] = "P = " + getUnitText(getPower(), "W");
        }

        public override string getScopeText(int v) {
            base.getScopeText(v);
            return "capacitor, " + getUnitText(capacitance, "F");
        }

        public override EditInfo getEditInfo(int n) {
            if (n == 0) {
                return new EditInfo("Capacitance (F)", capacitance, 0, 0);
            }
            if (n == 1) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.checkbox = new CheckBox();
                ei.checkbox.Text = "Trapezoidal Approximation";
                ei.checkbox.Checked = isTrapezoidal();
                return ei;
            }
            return null;
        }

        public override void setEditValue(int n, EditInfo ei) {
            if (n == 0 && ei.value > 0) {
                capacitance = ei.value;
            }
            if (n == 1) {
                if (ei.checkbox.Checked) {
                    flags &= ~FLAG_BACK_EULER;
                } else {
                    flags |= FLAG_BACK_EULER;
                }
            }
        }

        public override DUMP_ID getShortcut() { return DUMP_ID.CAPACITOR; }
    }
}
