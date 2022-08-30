using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Output {
    class LogicOutputUI : BaseUI {
        const int FLAG_TERNARY = 1;
        const int FLAG_NUMERIC = 2;
        const int FLAG_PULLDOWN = 4;

        public LogicOutputUI(Point pos) : base(pos) {
            Elm = new LogicOutputElm();
        }

        public LogicOutputUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new LogicOutputElm(st);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.LOGIC_O; } }

        bool isTernary { get { return (DumpInfo.Flags & FLAG_TERNARY) != 0; } }

        bool isNumeric { get { return (DumpInfo.Flags & (FLAG_TERNARY | FLAG_NUMERIC)) != 0; } }

        bool needsPullDown { get { return (DumpInfo.Flags & FLAG_PULLDOWN) != 0; } }

        public override void SetPoints() {
            base.SetPoints();
            setLead1(1 - 12 / mLen);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (LogicOutputElm)Elm;
            string s = (ce.Volts[0] < ce.mThreshold) ? "L" : "H";
            if (isTernary) {
                if (ce.Volts[0] > 3.75) {
                    s = "2";
                } else if (ce.Volts[0] > 1.25) {
                    s = "1";
                } else {
                    s = "0";
                }
            } else if (isNumeric) {
                s = (ce.Volts[0] < ce.mThreshold) ? "0" : "1";
            }
            ce.mValue = s;
            setBbox(mPost1, mLead1, 0);
            drawCenteredLText(s, DumpInfo.P2, true);
            drawLead(mPost1, mLead1);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            var ce = (LogicOutputElm)Elm;
            arr[0] = "logic output";
            arr[1] = (ce.Volts[0] < ce.mThreshold) ? "low" : "high";
            if (isNumeric) {
                arr[1] = ce.mValue;
            }
            arr[2] = "V = " + Utils.VoltageText(ce.Volts[0]);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (LogicOutputElm)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("閾値(V)", ce.mThreshold, 10, -10);
            }
            if (r == 1) {
                var ei = new ElementInfo("", 0, -1, -1);
                ei.CheckBox = new CheckBox() { Text = "プルダウン", Checked = needsPullDown };
                return ei;
            }
            if (r == 2) {
                var ei = new ElementInfo("", 0, 0, 0);
                ei.CheckBox = new CheckBox() { Text = "数値表示", Checked = isNumeric };
                return ei;
            }
            if (r == 3) {
                var ei = new ElementInfo("", 0, 0, 0);
                ei.CheckBox = new CheckBox() { Text = "3値", Checked = isTernary };
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (LogicOutputElm)Elm;
            if (n == 0) {
                ce.mThreshold = ei.Value;
            }
            if (n == 1) {
                if (ei.CheckBox.Checked) {
                    DumpInfo.Flags = FLAG_PULLDOWN;
                } else {
                    DumpInfo.Flags &= ~FLAG_PULLDOWN;
                }
                ce.needsPullDown = needsPullDown;
            }
            if (n == 2) {
                if (ei.CheckBox.Checked) {
                    DumpInfo.Flags |= FLAG_NUMERIC;
                } else {
                    DumpInfo.Flags &= ~FLAG_NUMERIC;
                }
            }
            if (n == 3) {
                if (ei.CheckBox.Checked) {
                    DumpInfo.Flags |= FLAG_TERNARY;
                } else {
                    DumpInfo.Flags &= ~FLAG_TERNARY;
                }
            }
        }
    }
}
