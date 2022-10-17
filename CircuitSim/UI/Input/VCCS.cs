using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Input;
using Circuit.UI.Custom;

namespace Circuit.UI.Input {
    class VCCS : Chip {
        public VCCS(Point pos, int dummy) : base(pos) { }

        public VCCS(Point p1, Point p2, int f) : base(p1, p2, f) { }

        public VCCS(Point pos) : base(pos) {
            Elm = new ElmVCCS(this);
            DumpInfo.ReferenceName = "VCCS";
        }

        public VCCS(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new ElmVCCS(this, st);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.VCCS; } }

        protected override void dump(List<object> optionList) {
            var ce = (ElmVCCS)Elm;
            /// TODO: baseList + " " + ce.InputCount + " " + Utils.Escape(ce.ExprString);
            base.dump(optionList);
            optionList.Add(ce.InputCount);
        }

        public override void Draw(CustomGraphics g) {
            drawChip(g);
        }

        public override void GetInfo(string[] arr) {
            base.GetInfo(arr);
            var ce = (ElmVCCS)Elm;
            int i;
            for (i = 0; arr[i] != null; i++)
                ;
            arr[i] = "I = " + Utils.CurrentText(ce.Pins[ce.InputCount].current);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmVCCS)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                var ei = new ElementInfo("Output Function");
                ei.Text = ce.ExprString;
                return ei;
            }
            if (r == 1) {
                return new ElementInfo("入力数", ce.InputCount);
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (ElmVCCS)Elm;
            if (n == 0) {
                ce.ExprString = ei.Textf.Text.Replace(" ", "").Replace("\r", "").Replace("\n", "");
                ce.ParseExpr();
                return;
            }
            if (n == 1) {
                if (ei.Value < 0 || ei.Value > 8) {
                    return;
                }
                ce.InputCount = (int)ei.Value;
                ce.SetupPins(this);
                ce.AllocNodes();
                SetPoints();
            }
        }
    }
}
