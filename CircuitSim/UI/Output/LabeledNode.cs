using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Output;

namespace Circuit.UI.Output {
    class LabeledNode : BaseUI {
        const int FLAG_INTERNAL = 1;
        const int CircleSize = 17;

        PointF mPos;

        public LabeledNode(Point pos) : base(pos) {
            Elm = new ElmLabeledNode();
            ReferenceName = "label";
        }

        public LabeledNode(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new ElmLabeledNode(st);
        }

        public bool IsInternal { get { return (mFlags & FLAG_INTERNAL) != 0; } }

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
            interpPost(ref mPos, 1 + 11.0 / mLen);
            setBbox(Elm.Post[0].X, Elm.Post[0].Y, (int)mPos.X, (int)mPos.Y, CircleSize);
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
            drawCenteredText(str, Post.B, true);
            if (lineOver) {
                int asc = (int)(CustomGraphics.TextSize + 0.5);
                if (lineOver) {
                    int ya = Post.B.Y - asc;
                    int sw = (int)g.GetTextSize(str).Width;
                    drawLine(Post.B.X - sw / 2, ya, Post.B.X + sw / 2, ya);
                }
            }
            updateDotCount(ce.Current, ref mCurCount);
            drawCurrentA(mCurCount);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmLabeledNode)Elm;
            arr[0] = ce.Text;
            arr[1] = "電流：" + Utils.CurrentText(ce.Current);
            arr[2] = "電位：" + Utils.VoltageText(ce.Volts[0]);
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
                mFlags = ei.ChangeFlag(mFlags, FLAG_INTERNAL);
            }
        }
    }
}
