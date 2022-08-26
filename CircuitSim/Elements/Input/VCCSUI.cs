using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Custom;

namespace Circuit.Elements.Input {
    class VCCSUI : ChipUI {
        public VCCSUI(Point pos, int dummy) : base(pos) { }

        public VCCSUI(Point p1, Point p2, int f) : base(p1, p2, f) { }

        public VCCSUI(Point pos) : base(pos) {
            Elm = new VCCSElm(this);
            DumpInfo.ReferenceName = "VCCS";
        }

        public VCCSUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new VCCSElm(this, st);
            try {
                DumpInfo.ReferenceName = st.nextToken();
            } catch { }
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.VCCS; } }

        protected override void dump(List<object> optionList) {
            var ce = (VCCSElm)Elm;
            var baseList = new List<object>();
            base.dump(baseList);
            /// TODO: baseList + " " + ce.InputCount + " " + Utils.Escape(ce.ExprString);
            optionList.AddRange(baseList);
            optionList.Add(ce.InputCount);
        }

        public override void Draw(CustomGraphics g) {
            drawChip(g);
        }

        public override void GetInfo(string[] arr) {
            base.GetInfo(arr);
            var ce = (VCCSElm)Elm;
            int i;
            for (i = 0; arr[i] != null; i++)
                ;
            arr[i] = "I = " + Utils.CurrentText(ce.Pins[ce.InputCount].current);
        }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (VCCSElm)Elm;
            if (n == 0) {
                var ei = new ElementInfo(ElementInfo.MakeLink("customfunction.html", "Output Function"), 0, -1, -1);
                ei.Text = ce.ExprString;
                ei.DisallowSliders();
                return ei;
            }
            if (n == 1) {
                return new ElementInfo("入力数", ce.InputCount, 1, 8).SetDimensionless();
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (VCCSElm)Elm;
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
