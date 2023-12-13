﻿using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Input;
using Circuit.UI.Passive;

namespace Circuit.UI.Input {
    class LogicInput : Switch {
        const int FLAG_NUMERIC = 2;

        bool isNumeric { get { return (mFlags & (FLAG_NUMERIC)) != 0; } }

        public LogicInput(Point pos) : base(pos, 0) {
            Elm = new ElmLogicInput();
        }

        public LogicInput(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new ElmLogicInput(st);
        }

        public override DUMP_ID DumpId { get { return DUMP_ID.LOGIC_I; } }

        protected override void dump(List<object> optionList) {
            optionList.Add(((ElmLogicInput)Elm).mHiV);
            optionList.Add(((ElmLogicInput)Elm).mLoV);
        }

        public override RectangleF GetSwitchRect() {
            return new RectangleF(Post.B.X - 10, Post.B.Y - 10, 20, 20);
        }

        public override void SetPoints() {
            base.SetPoints();
            setLead1(1 - 12 / Post.Len);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmLogicInput)Elm;
            string s = 0 != ce.Position ? "H" : "L";
            if (isNumeric) {
                s = "" + ce.Position;
            }
            drawCenteredLText(s, Post.B, true);
            drawLeadA();
            updateDotCount();
            drawCurrentA(mCurCount);
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmLogicInput)Elm;
            arr[0] = "ロジック入力";
            arr[1] = 0 != ce.Position ? "High" : "Low";
            if (isNumeric) {
                arr[1] = 0 != ce.Position ? "1" : "0";
            }
            arr[1] += " (" + Utils.VoltageText(ce.Volts[0]) + ")";
            arr[2] = "電流：" + Utils.CurrentText(ce.Current);
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
                return new ElementInfo("High電圧", ((ElmLogicInput)Elm).mHiV);
            }
            if (r == 2) {
                return new ElementInfo("Low電圧", ((ElmLogicInput)Elm).mLoV);
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
                    mFlags |= FLAG_NUMERIC;
                } else {
                    mFlags &= ~FLAG_NUMERIC;
                }
            }
        }
    }
}
