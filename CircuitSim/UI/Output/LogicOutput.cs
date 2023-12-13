using System.Drawing;

using Circuit.Elements.Output;

namespace Circuit.UI.Output {
    class LogicOutput : BaseUI {
        const int FLAG_TERNARY = 1;
        const int FLAG_NUMERIC = 2;
        const int FLAG_PULLDOWN = 4;

        public LogicOutput(Point pos) : base(pos) {
            Elm = new ElmLogicOutput();
        }

        public LogicOutput(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new ElmLogicOutput(st);
        }

        public override DUMP_ID DumpId { get { return DUMP_ID.LOGIC_O; } }

        bool isTernary { get { return (mFlags & FLAG_TERNARY) != 0; } }

        bool isNumeric { get { return (mFlags & (FLAG_TERNARY | FLAG_NUMERIC)) != 0; } }

        bool needsPullDown { get { return (mFlags & FLAG_PULLDOWN) != 0; } }

        public override void SetPoints() {
            base.SetPoints();
            setLead1(1 - 12 / Post.Len);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmLogicOutput)Elm;
            var s = (ce.Volts[0] < ce.mThreshold) ? "L" : "H";
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
            drawCenteredLText(s, Post.B, true);
            drawLeadA();
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmLogicOutput)Elm;
            arr[0] = "ロジック出力";
            arr[1] = (ce.Volts[0] < ce.mThreshold) ? "Low" : "High";
            if (isNumeric) {
                arr[1] = ce.mValue;
            }
            arr[2] = "電位：" + Utils.VoltageText(ce.Volts[0]);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmLogicOutput)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("閾値(V)", ce.mThreshold);
            }
            if (r == 1) {
                return new ElementInfo("プルダウン", needsPullDown);
            }
            if (r == 2) {
                return new ElementInfo("数値表示", isNumeric);
            }
            if (r == 3) {
                return new ElementInfo("3値", isTernary);
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (ElmLogicOutput)Elm;
            if (n == 0) {
                ce.mThreshold = ei.Value;
            }
            if (n == 1) {
                if (ei.CheckBox.Checked) {
                    mFlags = FLAG_PULLDOWN;
                } else {
                    mFlags &= ~FLAG_PULLDOWN;
                }
                ce.needsPullDown = needsPullDown;
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
