using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements;
using Circuit.Elements.Input;

using Circuit.UI.Passive;

namespace Circuit.UI.Input {
    class LogicInput : Switch {
        const int FLAG_NUMERIC = 2;

        bool isNumeric { get { return (DumpInfo.Flags & (FLAG_NUMERIC)) != 0; } }

        public LogicInput(Point pos) : base(pos, 0) {
            Elm = new ElmLogicInput();
        }

        public LogicInput(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new ElmLogicInput(st);
        }

        protected override int NumHandles { get { return 1; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.LOGIC_I; } }

        protected override void dump(List<object> optionList) {
            optionList.Add(((ElmLogicInput)Elm).mHiV);
            optionList.Add(((ElmLogicInput)Elm).mLoV);
        }

        public override Rectangle GetSwitchRect() {
            return new Rectangle(DumpInfo.P2.X - 10, DumpInfo.P2.Y - 10, 20, 20);
        }

        public override void SetPoints() {
            base.SetPoints();
            setLead1(1 - 12 / mLen);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmLogicInput)Elm;
            string s = 0 != ce.Position ? "H" : "L";
            if (isNumeric) {
                s = "" + ce.Position;
            }
            setBbox(mPost1, mLead1, 0);
            drawCenteredLText(s, DumpInfo.P2, true);
            drawLead(mPost1, mLead1);
            updateDotCount();
            drawDots(mPost1, mLead1, ce.CurCount);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmLogicInput)Elm;
            arr[0] = "logic input";
            arr[1] = 0 != ce.Position ? "high" : "low";
            if (isNumeric) {
                arr[1] = 0 != ce.Position ? "1" : "0";
            }
            arr[1] += " (" + Utils.VoltageText(ce.Volts[0]) + ")";
            arr[2] = "I = " + Utils.CurrentText(ce.Current);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmLogicInput)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("モーメンタリ", ce.Momentary);
            }
            if (r == 1) {
                return new ElementInfo("H電圧(V)", ((ElmLogicInput)Elm).mHiV, 10, -10);
            }
            if (r == 2) {
                return new ElementInfo("L電圧(V)", ((ElmLogicInput)Elm).mLoV, 10, -10);
            }
            if (r == 3) {
                return new ElementInfo("数値表示", isNumeric);
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (ElmLogicInput)Elm;
            if (n == 0) {
                ce.Momentary = ei.CheckBox.Checked;
            }
            if (n == 1) {
                ((ElmLogicInput)Elm).mHiV = ei.Value;
            }
            if (n == 2) {
                ((ElmLogicInput)Elm).mLoV = ei.Value;
            }
            if (n == 3) {
                if (ei.CheckBox.Checked) {
                    DumpInfo.Flags |= FLAG_NUMERIC;
                } else {
                    DumpInfo.Flags &= ~FLAG_NUMERIC;
                }
            }
        }
    }
}
