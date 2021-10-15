using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Active {
    class AnalogSwitchElm : CircuitElm {
        const int FLAG_INVERT = 1;
        double resistance;
        double r_on;
        double r_off;
        bool open;
        Point ps;
        Point point3;
        Point lead3;

        public AnalogSwitchElm(Point pos) : base(pos) {
            r_on = 20;
            r_off = 1e10;
        }

        public AnalogSwitchElm(Point a, Point b, int f, StringTokenizer st) : base(a, b, f) {
            r_on = 20;
            r_off = 1e10;
            try {
                r_on = double.Parse(st.nextToken());
                r_off = double.Parse(st.nextToken());
            } catch (Exception e) {
            }
        }

        protected override string dump() {
            return " " + r_on + " " + r_off;
        }

        protected override void calculateCurrent() {
            mCurrent = (Volts[0] - Volts[1]) / resistance;
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.ANALOG_SW; } }

        // we need this to be able to change the matrix for each step
        public override bool NonLinear { get { return true; } }

        public override int PostCount { get { return 3; } }

        public override Point GetPost(int n) {
            return (0 == n) ? mPoint1 : (1 == n) ? mPoint2 : point3;
        }

        // we have to just assume current will flow either way, even though that
        // might cause singular matrix errors
        public override bool GetConnection(int n1, int n2) {
            if (n1 == 2 || n2 == 2) {
                return false;
            }
            return true;
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

        public override void Stamp() {
            mCir.StampNonLinear(Nodes[0]);
            mCir.StampNonLinear(Nodes[1]);
        }

        public override void Drag(Point pos) {
            pos = CirSim.Sim.SnapGrid(pos);
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

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(32);
            ps = new Point();
            int openhs = 16;
            interpPoint(ref point3, 0.5, -openhs);
            interpPoint(ref lead3, 0.5, -openhs / 2);
        }

        public override void Draw(CustomGraphics g) {
            int openhs = 16;
            int hs = open ? openhs : 0;
            setBbox(mPoint1, mPoint2, openhs);

            draw2Leads(g);

            interpLead(ref ps, 1, hs);
            g.DrawThickLine(CustomGraphics.SelectColor, mLead1, ps);

            drawVoltage(g, 2, point3, lead3);

            if (!open) {
                doDots(g);
            }
            drawPosts(g);
        }

        public override void DoStep() {
            open = Volts[2] < 2.5;
            if ((mFlags & FLAG_INVERT) != 0) {
                open = !open;
            }
            resistance = open ? r_off : r_on;
            mCir.StampResistor(Nodes[0], Nodes[1], resistance);
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "analog switch";
            arr[1] = open ? "open" : "closed";
            arr[2] = "Vd = " + Utils.VoltageDText(VoltageDiff);
            arr[3] = "I = " + Utils.CurrentDText(Current);
            arr[4] = "Vc = " + Utils.VoltageText(Volts[2]);
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
                mFlags = ei.CheckBox.Checked ? (mFlags | FLAG_INVERT) : (mFlags & ~FLAG_INVERT);
            }
            if (n == 1 && ei.Value > 0) {
                r_on = ei.Value;
            }
            if (n == 2 && ei.Value > 0) {
                r_off = ei.Value;
            }
        }
    }
}
