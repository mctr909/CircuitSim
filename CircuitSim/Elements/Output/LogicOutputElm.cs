using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Output {
    class LogicOutputElm : CircuitElm {
        const int FLAG_TERNARY = 1;
        const int FLAG_NUMERIC = 2;
        const int FLAG_PULLDOWN = 4;

        double mThreshold;
        string mValue;

        public LogicOutputElm(Point pos) : base(pos) {
            mThreshold = 2.5;
        }

        public LogicOutputElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            try {
                mThreshold = st.nextTokenDouble();
            } catch {
                mThreshold = 2.5;
            }
        }

        public override double CirVoltageDiff { get { return CirVolts[0]; } }

        public override int CirPostCount { get { return 1; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.LOGIC_O; } }

        protected override string dump() {
            return mThreshold.ToString();
        }

        bool isTernary() { return (mFlags & FLAG_TERNARY) != 0; }

        bool isNumeric() { return (mFlags & (FLAG_TERNARY | FLAG_NUMERIC)) != 0; }

        bool needsPullDown() { return (mFlags & FLAG_PULLDOWN) != 0; }

        public override void CirStamp() {
            if (needsPullDown()) {
                mCir.StampResistor(CirNodes[0], 0, 1e6);
            }
        }

        public override void SetPoints() {
            base.SetPoints();
            setLead1(1 - 12 / mLen);
        }

        public override void Draw(CustomGraphics g) {
            string s = (CirVolts[0] < mThreshold) ? "L" : "H";
            if (isTernary()) {
                if (CirVolts[0] > 3.75) {
                    s = "2";
                } else if (CirVolts[0] > 1.25) {
                    s = "1";
                } else {
                    s = "0";
                }
            } else if (isNumeric()) {
                s = (CirVolts[0] < mThreshold) ? "0" : "1";
            }
            mValue = s;
            setBbox(mPoint1, mLead1, 0);
            drawCenteredLText(s, P2, true);
            drawLead(mPoint1, mLead1);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "logic output";
            arr[1] = (CirVolts[0] < mThreshold) ? "low" : "high";
            if (isNumeric()) {
                arr[1] = mValue;
            }
            arr[2] = "V = " + Utils.VoltageText(CirVolts[0]);
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("閾値(V)", mThreshold, 10, -10);
            }
            if (n == 1) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() { Text = "プルダウン", Checked = needsPullDown() };
                return ei;
            }
            if (n == 2) {
                var ei = new ElementInfo("", 0, 0, 0);
                ei.CheckBox = new CheckBox() { Text = "数値表示", Checked = isNumeric() };
                return ei;
            }
            if (n == 3) {
                var ei = new ElementInfo("", 0, 0, 0);
                ei.CheckBox = new CheckBox() { Text = "3値", Checked = isTernary() };
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0)
                mThreshold = ei.Value;
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
