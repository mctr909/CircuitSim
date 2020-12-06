using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements {
    class LogicOutputElm : CircuitElm {
        const int FLAG_TERNARY = 1;
        const int FLAG_NUMERIC = 2;
        const int FLAG_PULLDOWN = 4;
        double threshold;
        string value;

        public LogicOutputElm(int xx, int yy) : base(xx, yy) {
            threshold = 2.5;
        }

        public LogicOutputElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            try {
                threshold = st.nextTokenDouble();
            } catch {
                threshold = 2.5;
            }
        }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override int PostCount { get { return 1; } }

        protected override string dump() {
            return threshold.ToString();
        }

        protected override DUMP_ID getDumpType() { return DUMP_ID.LOGIC_O ; }

        bool isTernary() { return (mFlags & FLAG_TERNARY) != 0; }

        bool isNumeric() { return (mFlags & (FLAG_TERNARY | FLAG_NUMERIC)) != 0; }

        bool needsPullDown() { return (mFlags & FLAG_PULLDOWN) != 0; }

        public override void SetPoints() {
            base.SetPoints();
            mLead1 = interpPoint(mPoint1, mPoint2, 1 - 12 / mLen);
        }

        public override void Draw(CustomGraphics g) {
            string s = (Volts[0] < threshold) ? "L" : "H";
            if (isTernary()) {
                if (Volts[0] > 3.75) {
                    s = "2";
                } else if (Volts[0] > 1.25) {
                    s = "1";
                } else {
                    s = "0";
                }
            } else if (isNumeric()) {
                s = (Volts[0] < threshold) ? "0" : "1";
            }
            value = s;
            setBbox(mPoint1, mLead1, 0);
            drawCenteredLText(g, s, X2, Y2, true);
            g.DrawThickLine(getVoltageColor(Volts[0]), mPoint1, mLead1);
            drawPosts(g);
        }

        public override void Stamp() {
            if (needsPullDown()) {
                Cir.StampResistor(Nodes[0], 0, 1e6);
            }
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "logic output";
            arr[1] = (Volts[0] < threshold) ? "low" : "high";
            if (isNumeric()) {
                arr[1] = value;
            }
            arr[2] = "V = " + getVoltageText(Volts[0]);
        }

        public override EditInfo GetEditInfo(int n) {
            if (n == 0) {
                return new EditInfo("Threshold", threshold, 10, -10);
            }
            if (n == 1) {
                var ei = new EditInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() { Text = "Current Required", Checked = needsPullDown() };
                return ei;
            }
            if (n == 2) {
                var ei = new EditInfo("", 0, 0, 0);
                ei.CheckBox = new CheckBox() { Text = "Numeric", Checked = isNumeric() };
                return ei;
            }
            if (n == 3) {
                var ei = new EditInfo("", 0, 0, 0);
                ei.CheckBox = new CheckBox() { Text = "Ternary", Checked = isTernary() };
                return ei;
            }
            return null;
        }

        public override void SetEditValue(int n, EditInfo ei) {
            if (n == 0)
                threshold = ei.Value;
            if (n == 1) {
                if (ei.CheckBox.Checked) {
                    mFlags = FLAG_PULLDOWN;
                } else {
                    mFlags &= ~FLAG_PULLDOWN;
                }
            }
            if (n == 2) {
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_NUMERIC;
                } else {
                    mFlags &= ~FLAG_NUMERIC;
                }
            }
            if (n == 3) {
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_TERNARY;
                } else {
                    mFlags &= ~FLAG_TERNARY;
                }
            }
        }
    }
}
