using System.Windows.Forms;
using System.Drawing;

namespace Circuit.Elements {
    class InductorElm : CircuitElm {
        Inductor ind;
        public double inductance { get; private set; }

        public InductorElm(int xx, int yy) : base(xx, yy) {
            ind = new Inductor(sim, cir);
            inductance = 0.001;
            ind.setup(inductance, current, flags);
        }

        public InductorElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            ind = new Inductor(sim, cir);
            inductance = st.nextTokenDouble();
            current = st.nextTokenDouble();
            ind.setup(inductance, current, flags);
        }

        public double getInductance() { return inductance; }

        public void setInductance(double l) {
            inductance = l;
            ind.setup(inductance, current, flags);
        }

        public override DUMP_ID getDumpType() { return DUMP_ID.INDUCTOR; }

        public override string dump() {
            return base.dump() + " " + inductance + " " + current;
        }

        public override void setPoints() {
            base.setPoints();
            calcLeads(40);
        }

        public override void draw(Graphics g) {
            double v1 = volts[0];
            double v2 = volts[1];
            int hs = 8;
            setBbox(point1, point2, hs);

            draw2Leads(g);
            drawCoil(g, 8, lead1, lead2, v1, v2);

            if (sim.chkShowValuesCheckItem.Checked) {
                var s = getShortUnitText(inductance, "H");
                drawValues(g, s, hs);
            }
            doDots(g);
            drawPosts(g);
        }

        public override void reset() {
            current = volts[0] = volts[1] = curcount = 0;
            ind.reset();
        }

        public override void stamp() { ind.stamp(nodes[0], nodes[1]); }

        public override void startIteration() {
            ind.startIteration(volts[0] - volts[1]);
        }

        public override bool nonLinear() { return ind.nonLinear(); }

        public override void calculateCurrent() {
            double voltdiff = volts[0] - volts[1];
            current = ind.calculateCurrent(voltdiff);
        }

        public override void doStep() {
            double voltdiff = volts[0] - volts[1];
            ind.doStep(voltdiff);
        }

        public override void getInfo(string[] arr) {
            arr[0] = "inductor";
            getBasicInfo(arr);
            arr[3] = "L = " + getUnitText(inductance, "H");
            arr[4] = "P = " + getUnitText(getPower(), "W");
        }

        public override EditInfo getEditInfo(int n) {
            if (n == 0) {
                return new EditInfo("Inductance (H)", inductance, 0, 0);
            }
            if (n == 1) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.checkbox = new CheckBox();
                ei.checkbox.Text = "Trapezoidal Approximation";
                ei.checkbox.Checked = ind.isTrapezoidal();
                return ei;
            }
            return null;
        }

        public override void setEditValue(int n, EditInfo ei) {
            if (n == 0 && ei.value > 0) {
                inductance = ei.value;
            }
            if (n == 1) {
                if (ei.checkbox.Checked) {
                    flags &= ~Inductor.FLAG_BACK_EULER;
                } else {
                    flags |= Inductor.FLAG_BACK_EULER;
                }
            }
            ind.setup(inductance, current, flags);
        }

        public override DUMP_ID getShortcut() { return DUMP_ID.INDUCTOR; }
    }
}
