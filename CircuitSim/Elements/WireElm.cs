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

        public override DUMP_ID Shortcut { get { return DUMP_ID.WIRE; } }

        public override bool IsWire { get { return true; } }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override double Power { get { return 0; } }

        protected override string dump() { return ""; }

        protected override DUMP_ID getDumpType() { return DUMP_ID.WIRE; }

        public override void Draw(Graphics g) {
            drawThickLine(g, getVoltageColor(Volts[0]), mPoint1, mPoint2);
            doDots(g);
            setBbox(mPoint1, mPoint2, 3);
            string s = "";
            if (mustShowCurrent()) {
                s = getShortUnitText(Math.Abs(mCurrent), "A");
            }
            if (mustShowVoltage()) {
                s = (s.Length > 0 ? s + " " : "") + getShortUnitText(Volts[0], "V");
            }
            drawValues(g, s, 4);
            drawPosts(g);
        }

        public override void Stamp() {
            /*cir.stampVoltageSource(nodes[0], nodes[1], voltSource, 0);*/
        }

        bool mustShowCurrent() {
            return (mFlags & FLAG_SHOWCURRENT) != 0;
        }

        bool mustShowVoltage() {
            return (mFlags & FLAG_SHOWVOLTAGE) != 0;
        }

        /*public override int getVoltageSourceCount() { return 1; } */

        public override void GetInfo(string[] arr) {
            arr[0] = "wire";
            arr[1] = "I = " + getCurrentDText(mCurrent);
            arr[2] = "V = " + getVoltageText(Volts[0]);
        }

        public override EditInfo GetEditInfo(int n) {
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

        public override void SetEditValue(int n, EditInfo ei) {
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
    }
}
