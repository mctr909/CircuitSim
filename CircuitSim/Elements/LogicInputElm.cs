using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements {
    class LogicInputElm : SwitchElm {
        const int FLAG_TERNARY = 1;
        const int FLAG_NUMERIC = 2;
        double hiV;
        double loV;

        public LogicInputElm(Point pos) : base(pos, false) {
            mNumHandles = 1;
            hiV = 5;
            loV = 0;
        }

        public LogicInputElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
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

        public override double VoltageDiff { get { return Volts[0]; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override int PostCount { get { return 1; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.LOGIC_I; } }

        protected override string dump() {
            return base.dump() + " " + hiV + " " + loV;
        }

        bool isTernary() { return (mFlags & FLAG_TERNARY) != 0; }

        bool isNumeric() { return (mFlags & (FLAG_TERNARY | FLAG_NUMERIC)) != 0; }

        public override void SetPoints() {
            base.SetPoints();
            Utils.InterpPoint(mPoint1, mPoint2, ref mLead1, 1 - 12 / mLen);
        }

        public override void Draw(CustomGraphics g) {
            string s = position == 0 ? "L" : "H";
            if (isNumeric()) {
                s = "" + position;
            }
            setBbox(mPoint1, mLead1, 0);
            drawCenteredLText(g, s, P2.X, P2.Y, true);
            g.DrawThickLine(getVoltageColor(Volts[0]), mPoint1, mLead1);
            updateDotCount();
            drawDots(g, mPoint1, mLead1, mCurCount);
            drawPosts(g);
        }

        public override RectangleF getSwitchRect() {
            return new RectangleF(P2.X - 10, P2.Y - 10, 20, 20);
        }

        public override void SetCurrent(int vs, double c) { mCurrent = -c; }

        public override void Stamp() {
            double v = (position == 0) ? loV : hiV;
            if (isTernary()) {
                v = position * 2.5;
            }
            mCir.StampVoltageSource(0, Nodes[0], mVoltSource, v);
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "logic input";
            arr[1] = (position == 0) ? "low" : "high";
            if (isNumeric()) {
                arr[1] = "" + position;
            }
            arr[1] += " (" + Utils.VoltageText(Volts[0]) + ")";
            arr[2] = "I = " + Utils.CurrentText(mCurrent);
        }

        public override bool HasGroundConnection(int n1) { return true; }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                var ei = new ElementInfo("", 0, 0, 0);
                ei.CheckBox = new CheckBox() {
                    Text = "Momentary Switch",
                    Checked = momentary
                };
                return ei;
            }
            if (n == 1) {
                return new ElementInfo("High Voltage", hiV, 10, -10);
            }
            if (n == 2) {
                return new ElementInfo("Low Voltage", loV, 10, -10);
            }
            if (n == 3) {
                var ei = new ElementInfo("", 0, 0, 0);
                ei.CheckBox = new CheckBox() {
                    Text = "Numeric",
                    Checked = isNumeric()
                };
                return ei;
            }
            if (n == 4) {
                var ei = new ElementInfo("", 0, 0, 0);
                ei.CheckBox = new CheckBox() {
                    Text = "Ternary",
                    Checked = isTernary()
                };
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
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

        public override double GetCurrentIntoNode(int n) {
            return -mCurrent;
        }
    }
}
