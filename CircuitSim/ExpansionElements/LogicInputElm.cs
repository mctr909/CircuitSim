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
            numHandles = 1;
            hiV = 5;
            loV = 0;
        }

        public LogicInputElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f, st) {
            numHandles = 1;
            try {
                hiV = st.nextTokenDouble();
                loV = st.nextTokenDouble();
            } catch (Exception e) {
                hiV = 5;
                loV = 0;
            }
            if (isTernary()) {
                posCount = 3;
            }
        }

        bool isTernary() { return (flags & FLAG_TERNARY) != 0; }

        bool isNumeric() { return (flags & (FLAG_TERNARY | FLAG_NUMERIC)) != 0; }

        public override DUMP_ID getDumpType() { return DUMP_ID.LOGIC_I; }

        public override string dump() {
            return base.dump() + " " + hiV + " " + loV;
        }

        public override int getPostCount() { return 1; }

        public override void setPoints() {
            base.setPoints();
            lead1 = interpPoint(point1, point2, 1 - 12 / dn);
        }

        public override void draw(Graphics g) {
            string s = position == 0 ? "L" : "H";
            if (isNumeric()) {
                s = "" + position;
            }
            setBbox(point1, lead1, 0);
            drawCenteredText(g, s, x2, y2, true);
            PEN_THICK_LINE.Color = getVoltageColor(volts[0]);
            drawThickLine(g, point1, lead1);
            updateDotCount();
            drawDots(g, point1, lead1, curcount);
            drawPosts(g);
        }

        public override Rectangle getSwitchRect() {
            return new Rectangle(x2 - 10, y2 - 10, 20, 20);
        }

        public override void setCurrent(int vs, double c) { current = -c; }

        public override void stamp() {
            double v = (position == 0) ? loV : hiV;
            if (isTernary()) {
                v = position * 2.5;
            }
            cir.stampVoltageSource(0, nodes[0], voltSource, v);
        }

        public override int getVoltageSourceCount() { return 1; }

        public override double getVoltageDiff() { return volts[0]; }

        public override void getInfo(string[] arr) {
            arr[0] = "logic input";
            arr[1] = (position == 0) ? "low" : "high";
            if (isNumeric()) {
                arr[1] = "" + position;
            }
            arr[1] += " (" + getVoltageText(volts[0]) + ")";
            arr[2] = "I = " + getCurrentText(getCurrent());
        }

        public override bool hasGroundConnection(int n1) { return true; }

        public override EditInfo getEditInfo(int n) {
            if (n == 0) {
                var ei = new EditInfo("", 0, 0, 0);
                ei.checkbox = new CheckBox() {
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
                ei.checkbox = new CheckBox() {
                    Text = "Numeric",
                    Checked = isNumeric()
                };
                return ei;
            }
            if (n == 4) {
                var ei = new EditInfo("", 0, 0, 0);
                ei.checkbox = new CheckBox() {
                    Text = "Ternary",
                    Checked = isTernary()
                };
                return ei;
            }
            return null;
        }

        public override void setEditValue(int n, EditInfo ei) {
            if (n == 0) {
                momentary = ei.checkbox.Checked;
            }
            if (n == 1) {
                hiV = ei.value;
            }
            if (n == 2) {
                loV = ei.value;
            }
            if (n == 3) {
                if (ei.checkbox.Checked) {
                    flags |= FLAG_NUMERIC;
                } else {
                    flags &= ~FLAG_NUMERIC;
                }
            }
            if (n == 4) {
                if (ei.checkbox.Checked) {
                    flags |= FLAG_TERNARY;
                } else {
                    flags &= ~FLAG_TERNARY;
                }
                posCount = (isTernary()) ? 3 : 2;
            }
        }

        public override DUMP_ID getShortcut() { return DUMP_ID.LOGIC_I; }

        public override double getCurrentIntoNode(int n) {
            return -current;
        }
    }
}
