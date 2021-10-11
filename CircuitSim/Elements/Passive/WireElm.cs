using System;
using System.Windows.Forms;
using System.Drawing;

namespace Circuit.Elements.Passive {
    class WireElm : CircuitElm {
        const int FLAG_SHOWCURRENT = 1;
        const int FLAG_SHOWVOLTAGE = 2;

        Point mTextPos;
        public bool mHasWireInfo; /* used in CirSim to calculate wire currents */

        public WireElm(Point pos) : base(pos) { }

        public WireElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) { }

        public override DUMP_ID Shortcut { get { return DUMP_ID.WIRE; } }

        public override bool IsWire { get { return true; } }

        /*public override int VoltageSourceCount { get { return 1; } } */

        public override double VoltageDiff { get { return Volts[0]; } }

        public override double Power { get { return 0; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.WIRE; } }

        protected override string dump() { return ""; }

        bool mustShowCurrent { get { return (mFlags & FLAG_SHOWCURRENT) != 0; } }

        bool mustShowVoltage { get { return (mFlags & FLAG_SHOWVOLTAGE) != 0; } }

        public override void SetPoints() {
            base.SetPoints();
            int sign;
            if (mPoint1.Y == mPoint2.Y) {
                sign = mDsign;
            } else {
                sign = -mDsign;
            }
            interpPoint(ref mTextPos, 0.5 + 8 * sign / mLen, 15 * sign);
        }

        public override void Draw(CustomGraphics g) {
            drawVoltage(g, 0, mPoint1, mPoint2);
            doDots(g);
            setBbox(mPoint1, mPoint2, 3);
            string s = "";
            if (mustShowCurrent) {
                s = Utils.ShortUnitText(Math.Abs(mCurrent), "A");
            }
            if (mustShowVoltage) {
                s = (s.Length > 0 ? s + "\r\n" : "") + Utils.ShortUnitText(Volts[0], "V");
            }
            g.DrawRightText(s, mTextPos.X, mTextPos.Y);
            drawPosts(g);
        }

        public override void Stamp() {
            /*cir.stampVoltageSource(nodes[0], nodes[1], voltSource, 0);*/
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "wire";
            arr[1] = "I = " + Utils.CurrentDText(mCurrent);
            arr[2] = "V = " + Utils.VoltageText(Volts[0]);
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox();
                ei.CheckBox.Text = "Show Current";
                ei.CheckBox.Checked = mustShowCurrent;
                return ei;
            }
            if (n == 1) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox();
                ei.CheckBox.Text = "Show Voltage";
                ei.CheckBox.Checked = mustShowVoltage;
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
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
