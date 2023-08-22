using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Output;

namespace Circuit.UI.Output {
    class LabeledNode : BaseUI {
        const int FLAG_INTERNAL = 1;
        const int CircleSize = 17;

        Point mPos;

        public LabeledNode(Point pos) : base(pos) {
            Elm = new ElmLabeledNode();
            DumpInfo.ReferenceName = "label";
        }

        public LabeledNode(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new ElmLabeledNode(st);
        }

        public bool IsInternal { get { return (DumpInfo.Flags & FLAG_INTERNAL) != 0; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.LABELED_NODE; } }

        protected override void dump(List<object> optionList) {
            optionList.Add(((ElmLabeledNode)Elm).Text);
        }

        public override void SetPoints() {
            base.SetPoints();
            var ce = (ElmLabeledNode)Elm;
            if (mVertical) {
                setLead1(1 - 0.5 * Context.GetTextSize(ce.Text).Height / mLen);
            } else {
                setLead1(1 - 0.5 * Context.GetTextSize(ce.Text).Width / mLen);
            }
            interpPoint(ref mPos, 1 + 11.0 / mLen);
            setBbox(Elm.Post[0], mPos, CircleSize);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmLabeledNode)Elm;
            drawLeadA();
            var str = ce.Text;
            var lineOver = false;
            if (str.StartsWith("/")) {
                lineOver = true;
                str = str.Substring(1);
            }
            drawCenteredText(str, DumpInfo.P2.X, DumpInfo.P2.Y, true);
            if (lineOver) {
                int asc = (int)(CustomGraphics.TextSize + 0.5);
                if (lineOver) {
                    int ya = DumpInfo.P2.Y - asc;
                    int sw = (int)g.GetTextSize(str).Width;
                    drawLine(DumpInfo.P2.X - sw / 2, ya, DumpInfo.P2.X + sw / 2, ya);
                }
            }
            updateDotCount(ce.Current, ref CurCount);
            drawCurrentA(CurCount);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmLabeledNode)Elm;
            arr[0] = ce.Text;
            arr[1] = "I = " + Utils.CurrentText(ce.Current);
            arr[2] = "V = " + Utils.VoltageText(ce.Volts[0]);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmLabeledNode)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("名前", ce.Text);
            }
            if (r == 1) {
                return new ElementInfo("内部端子", IsInternal);
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (ElmLabeledNode)Elm;
            if (n == 0) {
                ce.Text = ei.Text;
            }
            if (n == 1) {
                DumpInfo.Flags = ei.ChangeFlag(DumpInfo.Flags, FLAG_INTERNAL);
            }
        }
    }
}
