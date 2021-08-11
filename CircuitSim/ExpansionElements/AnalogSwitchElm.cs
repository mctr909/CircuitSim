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

        PointF ps;
        Point point3;
        PointF lead3;

        public AnalogSwitchElm(Point pos) : base(pos) {
            r_on = 20;
            r_off = 1e10;
        }

        public AnalogSwitchElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
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

        public override DUMP_ID DumpType { get { return DUMP_ID.ANALOG_SW; } }

        protected override string dump() {
            return r_on + " " + r_off;
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(32);
            int openhs = 16;
            Utils.InterpPoint(mPoint1, mPoint2, ref point3, .5, -openhs);
            Utils.InterpPoint(mPoint1, mPoint2, ref lead3, .5, -openhs / 2);
        }

        public override void Draw(CustomGraphics g) {
            int openhs = 16;
            int hs = (open) ? openhs : 0;
            setBbox(mPoint1, mPoint2, openhs);

            draw2Leads(g);

            Utils.InterpPoint(mLead1, mLead2, ref ps, 1, hs);
            g.DrawThickLine(GrayColor, mLead1, ps);

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
            mCir.StampNonLinear(Nodes[0]);
            mCir.StampNonLinear(Nodes[1]);
        }

        public override void DoStep() {
            open = (Volts[2] < 2.5);
            if ((mFlags & FLAG_INVERT) != 0) {
                open = !open;
            }
            resistance = open ? r_off : r_on;
            mCir.StampResistor(Nodes[0], Nodes[1], resistance);
        }

        public override void Drag(Point pos) {
            pos = Sim.SnapGrid(pos);
            if (Math.Abs(P1.X - pos.X) < Math.Abs(P1.Y - pos.Y)) {
                pos.X = P1.X;
            } else {
                pos.Y = P1.Y;
            }
            int q1 = Math.Abs(P1.X - pos.X) + Math.Abs(P1.Y - pos.Y);
            int q2 = (q1 / 2) % CirSim.GRID_SIZE;
            if (q2 != 0) {
                return;
            }
            P2.X = pos.X;
            P2.Y = pos.Y;
            SetPoints();
        }

        public override Point GetPost(int n) {
            return (n == 0) ? mPoint1 : (n == 1) ? mPoint2 : point3;
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "analog switch";
            arr[1] = open ? "open" : "closed";
            arr[2] = "Vd = " + Utils.VoltageDText(VoltageDiff);
            arr[3] = "I = " + Utils.CurrentDText(mCurrent);
            arr[4] = "Vc = " + Utils.VoltageText(Volts[2]);
        }

        /* we have to just assume current will flow either way,
         * even though that might cause singular matrix errors */
        public override bool GetConnection(int n1, int n2) {
            if (n1 == 2 || n2 == 2) {
                return false;
            }
            return true;
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() {
                    Text = "Normally closed",
                    Checked = (mFlags & FLAG_INVERT) != 0
                };
                return ei;
            }
            if (n == 1) {
                return new ElementInfo("On Resistance (ohms)", r_on, 0, 0);
            }
            if (n == 2) {
                return new ElementInfo("Off Resistance (ohms)", r_off, 0, 0);
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
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
