﻿using System.Drawing;
using System.Windows.Forms;

using Circuit.Elements.Passive;

namespace Circuit.Elements.Input {
    class LogicInputElm : SwitchElm {
        const int FLAG_NUMERIC = 2;

        bool isNumeric { get { return (mFlags & (FLAG_NUMERIC)) != 0; } }

        public LogicInputElm(Point pos) : base(pos, 0) {
            CirElm = new LogicInputElmE();
        }

        public LogicInputElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            CirElm = new LogicInputElmE(st);
        }

        protected override int NumHandles { get { return 1; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.LOGIC_I; } }

        protected override string dump() {
            return " " + ((LogicInputElmE)CirElm).mHiV + " " + ((LogicInputElmE)CirElm).mLoV;
        }

        public override Rectangle GetSwitchRect() {
            return new Rectangle(P2.X - 10, P2.Y - 10, 20, 20);
        }

        public override void SetPoints() {
            base.SetPoints();
            setLead1(1 - 12 / mLen);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (LogicInputElmE)CirElm;
            string s = 0 != ce.Position ? "H" : "L";
            if (isNumeric) {
                s = "" + ce.Position;
            }
            setBbox(mPoint1, mLead1, 0);
            drawCenteredLText(s, P2, true);
            drawLead(mPoint1, mLead1);
            ce.cirUpdateDotCount();
            drawDots(mPoint1, mLead1, ce.CurCount);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            var ce = (LogicInputElmE)CirElm;
            arr[0] = "logic input";
            arr[1] = 0 != ce.Position ? "high" : "low";
            if (isNumeric) {
                arr[1] = 0 != ce.Position ? "1" : "0";
            }
            arr[1] += " (" + Utils.VoltageText(ce.Volts[0]) + ")";
            arr[2] = "I = " + Utils.CurrentText(ce.Current);
        }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (LogicInputElmE)CirElm;
            if (n == 0) {
                var ei = new ElementInfo("", 0, 0, 0);
                ei.CheckBox = new CheckBox() {
                    Text = "モーメンタリ",
                    Checked = ce.Momentary
                };
                return ei;
            }
            if (n == 1) {
                return new ElementInfo("H電圧(V)", ((LogicInputElmE)CirElm).mHiV, 10, -10);
            }
            if (n == 2) {
                return new ElementInfo("L電圧(V)", ((LogicInputElmE)CirElm).mLoV, 10, -10);
            }
            if (n == 3) {
                var ei = new ElementInfo("", 0, 0, 0);
                ei.CheckBox = new CheckBox() {
                    Text = "数値表示",
                    Checked = isNumeric
                };
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (LogicInputElmE)CirElm;
            if (n == 0) {
                ce.Momentary = ei.CheckBox.Checked;
            }
            if (n == 1) {
                ((LogicInputElmE)CirElm).mHiV = ei.Value;
            }
            if (n == 2) {
                ((LogicInputElmE)CirElm).mLoV = ei.Value;
            }
            if (n == 3) {
                if (ei.CheckBox.Checked) {
                    mFlags |= FLAG_NUMERIC;
                } else {
                    mFlags &= ~FLAG_NUMERIC;
                }
            }
        }
    }
}
