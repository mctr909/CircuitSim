using System.Windows.Forms;
using System.Drawing;

namespace Circuit.Elements {
    class InductorElm : CircuitElm {
        Inductor ind;
        public double inductance { get; private set; }

        public InductorElm(int xx, int yy) : base(xx, yy) {
            ind = new Inductor(Sim, Cir);
            inductance = 0.001;
            ind.setup(inductance, mCurrent, mFlags);
        }

        public InductorElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            ind = new Inductor(Sim, Cir);
            inductance = st.nextTokenDouble();
            mCurrent = st.nextTokenDouble();
            ind.setup(inductance, mCurrent, mFlags);
        }

        public double getInductance() { return inductance; }

        public void setInductance(double l) {
            inductance = l;
            ind.setup(inductance, mCurrent, mFlags);
        }

        public override DUMP_ID getDumpType() { return DUMP_ID.INDUCTOR; }

        public override string dump() {
            return base.dump() + " " + inductance + " " + mCurrent;
        }

        public override void setPoints() {
            base.setPoints();
            calcLeads(40);
        }

        public override void draw(Graphics g) {
            double v1 = Volts[0];
            double v2 = Volts[1];
            int hs = 8;
            setBbox(mPoint1, mPoint2, hs);

            draw2Leads(g);
            drawCoil(g, 8, mLead1, mLead2, v1, v2);

            if (Sim.chkShowValuesCheckItem.Checked) {
                var s = getShortUnitText(inductance, "H");
                drawValues(g, s, hs);
            }
            doDots(g);
            drawPosts(g);
        }

        public override void reset() {
            mCurrent = Volts[0] = Volts[1] = mCurCount = 0;
            ind.reset();
        }

        public override void stamp() { ind.stamp(Nodes[0], Nodes[1]); }

        public override void startIteration() {
            ind.startIteration(Volts[0] - Volts[1]);
        }

        public override bool nonLinear() { return ind.nonLinear(); }

        public override void calculateCurrent() {
            double voltdiff = Volts[0] - Volts[1];
            mCurrent = ind.calculateCurrent(voltdiff);
        }

        public override void doStep() {
            double voltdiff = Volts[0] - Volts[1];
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
                ei.CheckBox = new CheckBox();
                ei.CheckBox.Text = "Trapezoidal Approximation";
                ei.CheckBox.Checked = ind.isTrapezoidal();
                return ei;
            }
            return null;
        }

        public override void setEditValue(int n, EditInfo ei) {
            if (n == 0 && ei.Value > 0) {
                inductance = ei.Value;
            }
            if (n == 1) {
                if (ei.CheckBox.Checked) {
                    mFlags &= ~Inductor.FLAG_BACK_EULER;
                } else {
                    mFlags |= Inductor.FLAG_BACK_EULER;
                }
            }
            ind.setup(inductance, mCurrent, mFlags);
        }

        public override DUMP_ID getShortcut() { return DUMP_ID.INDUCTOR; }
    }
}
