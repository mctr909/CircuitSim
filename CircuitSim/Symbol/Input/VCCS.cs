﻿using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Input;
using Circuit.Symbol.Custom;

namespace Circuit.Symbol.Input {
    class VCCS : Chip {
        protected ElmVCCS mElm;

        public override BaseElement Element { get { return mElm; } }

        public VCCS(Point pos, int dummy) : base(pos) { }

        public VCCS(Point p1, Point p2, int f) : base(p1, p2, f) { }

        public VCCS(Point pos) : base(pos) {
            mElm = new ElmVCCS(this);
            ReferenceName = "VCCS";
        }

        public VCCS(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            mElm = new ElmVCCS(this, st);
        }

        public override DUMP_ID DumpId { get { return DUMP_ID.VCCS; } }

        protected override void dump(List<object> optionList) {
            /// TODO: baseList + " " + mElm.InputCount + " " + Utils.Escape(mElm.ExprString);
            base.dump(optionList);
            optionList.Add(mElm.InputCount);
        }

        public override void Draw(CustomGraphics g) {
            drawChip(g);
        }

        public override void GetInfo(string[] arr) {
            base.GetInfo(arr);
            int i;
            for (i = 0; arr[i] != null; i++);
            arr[i] = "I = " + Utils.CurrentText(mElm.Pins[mElm.InputCount].current);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                var ei = new ElementInfo("Output Function");
                ei.Text = "";
                return ei;
            }
            if (r == 1) {
                return new ElementInfo("入力数", mElm.InputCount);
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            if (n == 0) {
                //mElm.ExprString = ei.Text.Replace(" ", "").Replace("\r", "").Replace("\n", "");
                //mElm.ParseExpr();
                return;
            }
            if (n == 1) {
                if (ei.Value < 0 || ei.Value > 8) {
                    return;
                }
                mElm.InputCount = (int)ei.Value;
                mElm.SetupPins(this);
                mElm.AllocNodes();
                SetPoints();
            }
        }
    }
}
