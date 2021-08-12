using System.Drawing;
using System.Windows.Forms;

namespace Circuit.LogicElements {
    class LogicOutputElm : CircuitElm {
        const int FLAG_TERNARY = 1;
        const int FLAG_NUMERIC = 2;
        const int FLAG_PULLDOWN = 4;
        double threshold;
        string value;

        public LogicOutputElm(Point pos) : base(pos) {
            threshold = 2.5;
        }

        public LogicOutputElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            try {
                threshold = st.nextTokenDouble();
            } catch {
                threshold = 2.5;
            }
        }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override int PostCount { get { return 1; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.LOGIC_O; } }

        protected override string dump() {
            return threshold.ToString();
        }

        bool isTernary() { return (mFlags & FLAG_TERNARY) != 0; }

        bool isNumeric() { return (mFlags & (FLAG_TERNARY | FLAG_NUMERIC)) != 0; }

        bool needsPullDown() { return (mFlags & FLAG_PULLDOWN) != 0; }

        public override void SetPoints() {
            base.SetPoints();
            Utils.InterpPoint(mPoint1, mPoint2, ref mLead1, 1 - 12 / mLen);
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
            drawCenteredLText(g, s, P2.X, P2.Y, true);
            g.DrawThickLine(getVoltageColor(Volts[0]), mPoint1, mLead1);
            drawPosts(g);
        }

        public override void Stamp() {
            if (needsPullDown()) {
                mCir.StampResistor(Nodes[0], 0, 1e6);
            }
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "logic output";
            arr[1] = (Volts[0] < threshold) ? "low" : "high";
            if (isNumeric()) {
                arr[1] = value;
            }
            arr[2] = "V = " + Utils.VoltageText(Volts[0]);
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("Threshold", threshold, 10, -10);
            }
            if (n == 1) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() { Text = "Current Required", Checked = needsPullDown() };
                return ei;
            }
            if (n == 2) {
                var ei = new ElementInfo("", 0, 0, 0);
                ei.CheckBox = new CheckBox() { Text = "Numeric", Checked = isNumeric() };
                return ei;
            }
            if (n == 3) {
                var ei = new ElementInfo("", 0, 0, 0);
                ei.CheckBox = new CheckBox() { Text = "Ternary", Checked = isTernary() };
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
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
