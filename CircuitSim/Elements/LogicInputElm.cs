using System;
using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements {
    class LogicInputElm : SwitchElm {
        const int FLAG_TERNARY = 1;
        const int FLAG_NUMERIC = 2;
        double hiV;
        double loV;

        public LogicInputElm(int xx, int yy) : base(xx, yy, false) {
            mNumHandles = 1;
            hiV = 5;
            loV = 0;
        }

        public LogicInputElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f, st) {
            mNumHandles = 1;
            try {
                hiV = st.nextTokenDouble();
                loV = st.nextTokenDouble();
            } catch {
                hiV = 5;
                loV = 0;
            }
            if (isTernary()) {
                posCount = 3;
            }
        }

        protected override string dump() {
            return base.dump() + " " + hiV + " " + loV;
        }

        protected override DUMP_ID getDumpType() { return DUMP_ID.LOGIC_I; }

        bool isTernary() { return (mFlags & FLAG_TERNARY) != 0; }

        bool isNumeric() { return (mFlags & (FLAG_TERNARY | FLAG_NUMERIC)) != 0; }

        public override int getPostCount() { return 1; }

        public override void setPoints() {
            base.setPoints();
            mLead1 = interpPoint(mPoint1, mPoint2, 1 - 12 / mLen);
        }

        public override void draw(Graphics g) {
            string s = position == 0 ? "L" : "H";
            if (isNumeric()) {
                s = "" + position;
            }
            setBbox(mPoint1, mLead1, 0);
            drawCenteredText(g, s, X2, Y2, true);
            PenThickLine.Color = getVoltageColor(Volts[0]);
            drawThickLine(g, mPoint1, mLead1);
            updateDotCount();
            drawDots(g, mPoint1, mLead1, mCurCount);
            drawPosts(g);
        }

        public override Rectangle getSwitchRect() {
            return new Rectangle(X2 - 10, Y2 - 10, 20, 20);
        }

        public override void setCurrent(int vs, double c) { mCurrent = -c; }

        public override void stamp() {
            double v = (position == 0) ? loV : hiV;
            if (isTernary()) {
                v = position * 2.5;
            }
            Cir.StampVoltageSource(0, Nodes[0], mVoltSource, v);
        }

        public override int getVoltageSourceCount() { return 1; }

        public override double getVoltageDiff() { return Volts[0]; }

        public override void getInfo(string[] arr) {
            arr[0] = "logic input";
            arr[1] = (position == 0) ? "low" : "high";
            if (isNumeric()) {
                arr[1] = "" + position;
            }
            arr[1] += " (" + getVoltageText(Volts[0]) + ")";
            arr[2] = "I = " + getCurrentText(getCurrent());
        }

        public override bool hasGroundConnection(int n1) { return true; }

        public override EditInfo getEditInfo(int n) {
            if (n == 0) {
                var ei = new EditInfo("", 0, 0, 0);
                ei.CheckBox = new CheckBox() {
                    Text = "Momentary Switch",
                    Checked = momentary
                };
                return ei;
            }
            if (n == 1) {
                return new EditInfo("High Voltage", hiV, 10, -10);
            }
            if (n == 2) {
                return new EditInfo("Low Voltage", loV, 10, -10);
            }
            if (n == 3) {
                var ei = new EditInfo("", 0, 0, 0);
                ei.CheckBox = new CheckBox() {
                    Text = "Numeric",
                    Checked = isNumeric()
                };
                return ei;
            }
            if (n == 4) {
                var ei = new EditInfo("", 0, 0, 0);
                ei.CheckBox = new CheckBox() {
                    Text = "Ternary",
                    Checked = isTernary()
                };
                return ei;
            }
            return null;
        }

        public override void setEditValue(int n, EditInfo ei) {
            if (n == 0) {
                momentary = ei.CheckBox.Checked;
            }
            if (n == 1) {
                hiV = ei.Value;
            }
            if (n == 2) {
                loV = ei.Value;
            }
            if (n == 3) {
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_NUMERIC;
                } else {
                    mFlags &= ~FLAG_NUMERIC;
                }
            }
            if (n == 4) {
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_TERNARY;
                } else {
                    mFlags &= ~FLAG_TERNARY;
                }
                posCount = (isTernary()) ? 3 : 2;
            }
        }

        public override DUMP_ID getShortcut() { return DUMP_ID.LOGIC_I; }

        public override double getCurrentIntoNode(int n) {
            return -mCurrent;
        }
    }
}
