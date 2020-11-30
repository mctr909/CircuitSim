using System;
using System.Windows.Forms;
using System.Drawing;

namespace Circuit.Elements {
    class WireElm : CircuitElm {
        const int FLAG_SHOWCURRENT = 1;
        const int FLAG_SHOWVOLTAGE = 2;

        public bool hasWireInfo; /* used in CirSim to calculate wire currents */

        public WireElm(int xx, int yy) : base(xx, yy) { }

        public WireElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) { }

        public override void draw(Graphics g) {
            drawThickLine(g, getVoltageColor(Volts[0]), mPoint1, mPoint2);
            doDots(g);
            setBbox(mPoint1, mPoint2, 3);
            string s = "";
            if (mustShowCurrent()) {
                s = getShortUnitText(Math.Abs(getCurrent()), "A");
            }
            if (mustShowVoltage()) {
                s = (s.Length > 0 ? s + " " : "") + getShortUnitText(Volts[0], "V");
            }
            drawValues(g, s, 4);
            drawPosts(g);
        }

        public override void stamp() {
            /*cir.stampVoltageSource(nodes[0], nodes[1], voltSource, 0);*/
        }

        bool mustShowCurrent() {
            return (mFlags & FLAG_SHOWCURRENT) != 0;
        }

        bool mustShowVoltage() {
            return (mFlags & FLAG_SHOWVOLTAGE) != 0;
        }

        /*public override int getVoltageSourceCount() { return 1; } */

        public override void getInfo(string[] arr) {
            arr[0] = "wire";
            arr[1] = "I = " + getCurrentDText(getCurrent());
            arr[2] = "V = " + getVoltageText(Volts[0]);
        }

        public override DUMP_ID getDumpType() { return DUMP_ID.WIRE; }

        public override  double getPower() { return 0; }

        public override double getVoltageDiff() { return Volts[0]; }

        public override bool isWire() { return true; }

        public override EditInfo getEditInfo(int n) {
            if (n == 0) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox();
                ei.CheckBox.Text = "Show Current";
                ei.CheckBox.Checked = mustShowCurrent();
                return ei;
            }
            if (n == 1) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox();
                ei.CheckBox.Text = "Show Voltage";
                ei.CheckBox.Checked = mustShowVoltage();
                return ei;
            }
            return null;
        }

        public override void setEditValue(int n, EditInfo ei) {
            if (n == 0) {
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_SHOWCURRENT;
                } else {
                    mFlags &= ~FLAG_SHOWCURRENT;
                }
            }
            if (n == 1) {
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_SHOWVOLTAGE;
                } else {
                    mFlags &= ~FLAG_SHOWVOLTAGE;
                }
            }
        }

        public override DUMP_ID getShortcut() { return DUMP_ID.WIRE; }
    }
}
