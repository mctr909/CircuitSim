using System.Drawing;
using System.Windows.Forms;

using Circuit.Elements.Passive;

namespace Circuit.Elements.Input {
    class LogicInputElm : SwitchElm {
        const int FLAG_TERNARY = 1;
        const int FLAG_NUMERIC = 2;
        double mHiV;
        double mLoV;

        public LogicInputElm(Point pos) : base(pos, false) {
            mHiV = 5;
            mLoV = 0;
        }

        public LogicInputElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
            try {
                mHiV = st.nextTokenDouble();
                mLoV = st.nextTokenDouble();
            } catch {
                mHiV = 5;
                mLoV = 0;
            }
            if (isTernary()) {
                PosCount = 3;
            }
        }

        protected override int NumHandles { get { return 1; } }

        public override double VoltageDiff { get { return Volts[0]; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override int PostCount { get { return 1; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.LOGIC_I; } }

        protected override string dump() {
            return mHiV + " " + mLoV;
        }

        bool isTernary() { return (mFlags & FLAG_TERNARY) != 0; }

        bool isNumeric() { return (mFlags & (FLAG_TERNARY | FLAG_NUMERIC)) != 0; }

        public override bool HasGroundConnection(int n1) { return true; }

        public override double GetCurrentIntoNode(int n) {
            return -mCurrent;
        }

        public override Rectangle GetSwitchRect() {
            return new Rectangle(P2.X - 10, P2.Y - 10, 20, 20);
        }

        public override void SetCurrent(int vs, double c) { mCurrent = -c; }

        public override void Stamp() {
            double v = (Position == 0) ? mLoV : mHiV;
            if (isTernary()) {
                v = Position * 2.5;
            }
            mCir.StampVoltageSource(0, Nodes[0], mVoltSource, v);
        }

        public override void SetPoints() {
            base.SetPoints();
            setLead1(1 - 12 / mLen);
        }

        public override void Draw(CustomGraphics g) {
            string s = Position == 0 ? "L" : "H";
            if (isNumeric()) {
                s = "" + Position;
            }
            setBbox(mPoint1, mLead1, 0);
            drawCenteredLText(s, P2, true);
            drawVoltage(0, mPoint1, mLead1);
            updateDotCount();
            drawDots(mPoint1, mLead1, mCurCount);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "logic input";
            arr[1] = (Position == 0) ? "low" : "high";
            if (isNumeric()) {
                arr[1] = "" + Position;
            }
            arr[1] += " (" + Utils.VoltageText(Volts[0]) + ")";
            arr[2] = "I = " + Utils.CurrentText(mCurrent);
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                var ei = new ElementInfo("", 0, 0, 0);
                ei.CheckBox = new CheckBox() {
                    Text = "モーメンタリ",
                    Checked = Momentary
                };
                return ei;
            }
            if (n == 1) {
                return new ElementInfo("H電圧(V)", mHiV, 10, -10);
            }
            if (n == 2) {
                return new ElementInfo("L電圧(V)", mLoV, 10, -10);
            }
            if (n == 3) {
                var ei = new ElementInfo("", 0, 0, 0);
                ei.CheckBox = new CheckBox() {
                    Text = "数値表示",
                    Checked = isNumeric()
                };
                return ei;
            }
            if (n == 4) {
                var ei = new ElementInfo("", 0, 0, 0);
                ei.CheckBox = new CheckBox() {
                    Text = "3値",
                    Checked = isTernary()
                };
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0) {
                Momentary = ei.CheckBox.Checked;
            }
            if (n == 1) {
                mHiV = ei.Value;
            }
            if (n == 2) {
                mLoV = ei.Value;
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
                PosCount = (isTernary()) ? 3 : 2;
            }
        }
    }
}
