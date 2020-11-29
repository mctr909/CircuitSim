using System;
using System.Drawing;

namespace Circuit.Elements {
    class CurrentElm : CircuitElm {
        Point[] arrow;
        Point ashaft1;
        Point ashaft2;
        Point center;
        double currentValue;

        public CurrentElm(int xx, int yy) : base(xx, yy) {
            currentValue = .01;
        }

        public CurrentElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            try {
                currentValue = st.nextTokenDouble();
            } catch {
                currentValue = .01;
            }
        }

        public override string dump() {
            return base.dump() + " " + currentValue;
        }

        public override DUMP_ID getDumpType() { return DUMP_ID.CURRENT; }

        public override void setPoints() {
            base.setPoints();
            calcLeads(36);
            ashaft1 = interpPoint(lead1, lead2, .25);
            ashaft2 = interpPoint(lead1, lead2, .6);
            center = interpPoint(lead1, lead2, .5);
            var p2 = interpPoint(lead1, lead2, .8);
            arrow = calcArrow(center, p2, 8, 6).ToArray();
        }

        public override void draw(Graphics g) {
            int cr = 32;
            draw2Leads(g);

            PEN_THICK_LINE.Color = getVoltageColor((volts[0] + volts[1]) / 2);
            drawThickCircle(g, center.X, center.Y, cr);
            drawThickLine(g, ashaft1, ashaft2);
            fillPolygon(g, PEN_THICK_LINE.Color, arrow);

            setBbox(point1, point2, cr);
            doDots(g);
            if (sim.chkShowValuesCheckItem.Checked) {
                string s = getShortUnitText(currentValue, "A");
                if (dx == 0 || dy == 0) {
                    drawValues(g, s, cr);
                }
            }
            drawPosts(g);
        }

        /* we defer stamping current sources until we can tell if they have a current path or not */
        public void stampCurrentSource(bool broken) {
            if (broken) {
                /* no current path; stamping a current source would cause a matrix error. */
                cir.stampResistor(nodes[0], nodes[1], 1e8);
                current = 0;
            } else {
                /* ok to stamp a current source */
                cir.stampCurrentSource(nodes[0], nodes[1], currentValue);
                current = currentValue;
            }
        }

        public override EditInfo getEditInfo(int n) {
            if (n == 0) {
                return new EditInfo("Current (A)", currentValue, 0, .1);
            }
            return null;
        }

        public override void setEditValue(int n, EditInfo ei) {
            currentValue = ei.value;
        }

        public override void getInfo(string[] arr) {
            arr[0] = "current source";
            getBasicInfo(arr);
        }

        public override double getVoltageDiff() {
            return volts[1] - volts[0];
        }

        public override double getPower() { return -getVoltageDiff() * current; }
    }
}
