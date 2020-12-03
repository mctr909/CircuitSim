using System.Drawing;

namespace Circuit.Elements {
    class ResistorElm : CircuitElm {
        public double resistance { get; private set; }

        Point ps1;
        Point ps2;
        Point ps3;
        Point ps4;

        public ResistorElm(int xx, int yy) : base(xx, yy) {
            resistance = 1000;
        }

        public ResistorElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            resistance = st.nextTokenDouble();
        }

        double getResistance() { return resistance; }

        void setResistance(double r) { resistance = r; }

        protected override string dump() {
            return resistance.ToString();
        }

        protected override DUMP_ID getDumpType() { return DUMP_ID.RESISTOR; }

        public override void setPoints() {
            base.setPoints();
            calcLeads(32);
        }

        public override void draw(Graphics g) {
            var len = (float)distance(mLead1, mLead2);
            if (0 == len) {
                return;
            }

            const int hs = 5;
            setBbox(mPoint1, mPoint2, hs);

            draw2Leads(g);

            int segments = 12;
            double segf = 1.0 / segments;
            double v1 = Volts[0];
            double v2 = Volts[1];

            if (Sim.chkAnsiResistorCheckItem.Checked) {
                /* draw zigzag */
                int oy = 0;
                int ny;
                for (int i = 0; i != segments; i++) {
                    switch (i & 3) {
                    case 0: ny = hs; break;
                    case 2: ny = -hs; break;
                    default: ny = 0; break;
                    }
                    interpPoint(mLead1, mLead2, ref ps1, i * segf, oy);
                    interpPoint(mLead1, mLead2, ref ps2, (i + 1) * segf, ny);
                    double v = v1 + (v2 - v1) * i / segments;
                    drawThickLine(g, getVoltageColor(v), ps1, ps2);
                    oy = ny;
                }
            } else {
                /* draw rectangle */
                PenThickLine.Color = getVoltageColor(v1);
                interpPoint(mLead1, mLead2, ref ps1, ref ps2, 0, hs);
                drawThickLine(g, ps1, ps2);
                for (int i = 0; i != segments; i++) {
                    double v = v1 + (v2 - v1) * i / segments;
                    interpPoint(mLead1, mLead2, ref ps1, ref ps2, i * segf, hs);
                    interpPoint(mLead1, mLead2, ref ps3, ref ps4, (i + 1) * segf, hs);
                    PenThickLine.Color = getVoltageColor(v);
                    drawThickLine(g, ps1, ps3);
                    drawThickLine(g, ps2, ps4);
                }
                interpPoint(mLead1, mLead2, ref ps1, ref ps2, 1, hs);
                drawThickLine(g, ps1, ps2);
            }

            if (Sim.chkShowValuesCheckItem.Checked) {
                var s = getShortUnitText(resistance, "");
                drawValues(g, s, hs + 2);
            }

            doDots(g);
            drawPosts(g);
        }

        public override void calculateCurrent() {
            mCurrent = (Volts[0] - Volts[1]) / resistance;
            /*Console.WriteLine(this + " res current set to " + current + "\n");*/
        }

        public override void stamp() {
            Cir.StampResistor(Nodes[0], Nodes[1], resistance);
        }

        public override void getInfo(string[] arr) {
            arr[0] = "resistor";
            getBasicInfo(arr);
            arr[3] = "R = " + getUnitText(resistance, CirSim.ohmString);
            arr[4] = "P = " + getUnitText(getPower(), "W");
        }

        public override string getScopeText(int v) {
            return "resistor, " + getUnitText(resistance, CirSim.ohmString);
        }

        public override EditInfo getEditInfo(int n) {
            /* ohmString doesn't work here on linux */
            if (n == 0) {
                return new EditInfo("Resistance (ohms)", resistance, 0, 0);
            }
            return null;
        }

        public override void setEditValue(int n, EditInfo ei) {
            if (ei.Value > 0) {
                resistance = ei.Value;
            }
        }

        public override DUMP_ID getShortcut() { return DUMP_ID.RESISTOR; }
    }
}
