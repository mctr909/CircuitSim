using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements {
    class AnalogSwitchElm : CircuitElm {
        const int FLAG_INVERT = 1;
        double resistance;
        double r_on;
        double r_off;

        bool open;

        Point ps;
        Point point3;
        Point lead3;

        public AnalogSwitchElm(int xx, int yy) : base(xx, yy) {
            r_on = 20;
            r_off = 1e10;
        }

        public AnalogSwitchElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            try {
                r_on = st.nextTokenDouble();
                r_off = st.nextTokenDouble();
            } catch {
                r_on = 20;
                r_off = 1e10;
            }
        }

        /* we need this to be able to change the matrix for each step */
        public override bool NonLinear { get { return true; } }

        public override int PostCount { get { return 3; } }

        protected override string dump() {
            return r_on + " " + r_off;
        }

        protected override DUMP_ID getDumpType() { return DUMP_ID.ANALOG_SW; }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(32);
            int openhs = 16;
            point3 = interpPoint(mPoint1, mPoint2, .5, -openhs);
            lead3 = interpPoint(mPoint1, mPoint2, .5, -openhs / 2);
        }

        public override void Draw(CustomGraphics g) {
            int openhs = 16;
            int hs = (open) ? openhs : 0;
            setBbox(mPoint1, mPoint2, openhs);

            draw2Leads(g);

            interpPoint(mLead1, mLead2, ref ps, 1, hs);
            g.DrawThickLine(LightGrayColor, mLead1, ps);

            g.DrawThickLine(getVoltageColor(Volts[2]), point3, lead3);

            if (!open) {
                doDots(g);
            }
            drawPosts(g);
        }

        protected override void calculateCurrent() {
            mCurrent = (Volts[0] - Volts[1]) / resistance;
        }

        public override void Stamp() {
            Cir.StampNonLinear(Nodes[0]);
            Cir.StampNonLinear(Nodes[1]);
        }

        public override void DoStep() {
            open = (Volts[2] < 2.5);
            if ((mFlags & FLAG_INVERT) != 0) {
                open = !open;
            }
            resistance = open ? r_off : r_on;
            Cir.StampResistor(Nodes[0], Nodes[1], resistance);
        }

        public override void Drag(int xx, int yy) {
            xx = Sim.snapGrid(xx);
            yy = Sim.snapGrid(yy);
            if (Math.Abs(X1 - xx) < Math.Abs(Y1 - yy)) {
                xx = X1;
            } else {
                yy = Y1;
            }
            int q1 = Math.Abs(X1 - xx) + Math.Abs(Y1 - yy);
            int q2 = (q1 / 2) % Sim.gridSize;
            if (q2 != 0) {
                return;
            }
            X2 = xx; X2 = yy;
            SetPoints();
        }

        public override Point GetPost(int n) {
            return (n == 0) ? mPoint1 : (n == 1) ? mPoint2 : point3;
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "analog switch";
            arr[1] = open ? "open" : "closed";
            arr[2] = "Vd = " + getVoltageDText(VoltageDiff);
            arr[3] = "I = " + getCurrentDText(mCurrent);
            arr[4] = "Vc = " + getVoltageText(Volts[2]);
        }

        /* we have to just assume current will flow either way,
         * even though that might cause singular matrix errors */
        public override bool GetConnection(int n1, int n2) {
            if (n1 == 2 || n2 == 2) {
                return false;
            }
            return true;
        }

        public override EditInfo GetEditInfo(int n) {
            if (n == 0) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    Text = "Normally closed",
                    Checked = (mFlags & FLAG_INVERT) != 0
                };
                return ei;
            }
            if (n == 1) {
                return new EditInfo("On Resistance (ohms)", r_on, 0, 0);
            }
            if (n == 2) {
                return new EditInfo("Off Resistance (ohms)", r_off, 0, 0);
            }
            return null;
        }

        public override void SetEditValue(int n, EditInfo ei) {
            if (n == 0) {
                mFlags = (ei.CheckBox.Checked) ? (mFlags | FLAG_INVERT) : (mFlags & ~FLAG_INVERT);
            }
            if (n == 1 && ei.Value > 0) {
                r_on = ei.Value;
            }
            if (n == 2 && ei.Value > 0) {
                r_off = ei.Value;
            }
        }

        public override double GetCurrentIntoNode(int n) {
            if (n == 2) {
                return 0;
            }
            if (n == 0) {
                return -mCurrent;
            }
            return mCurrent;
        }
    }
}
