using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Output;

namespace Circuit.UI.Output {
    class LabeledNode : BaseUI {
        const int FLAG_INTERNAL = 1;
        const int LabelSize = 17;

        PointF[] mTextPoly;

        public LabeledNode(Point pos) : base(pos) {
            Elm = new ElmLabeledNode();
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
            setTextPos();
        }

        void setTextPos() {
            var ce = (ElmLabeledNode)Elm;
            var txtW = Context.GetTextSize(ce.Text).Width;
            var txtH = Context.GetTextSize(ce.Text).Height;
            var pw = txtW / Post.Len;
            var ph = 0.5 * (txtH - 1);
            setLead1(1);
            Post.SetBbox(Post.A, Post.B, txtH);
            var p1 = new PointF();
            var p2 = new PointF();
            var p3 = new PointF();
            var p4 = new PointF();
            var p5 = new PointF();
            interpPost(ref p1, 1, -ph);
            interpPost(ref p2, 1, ph);
            interpPost(ref p3, 1 + pw, ph);
            interpPost(ref p4, 1 + pw + ph / Post.Len, 0);
            interpPost(ref p5, 1 + pw, -ph);
            mTextPoly = new PointF[] {
                p1, p2, p3, p4, p5, p1
            };
            var abX = Post.B.X - Post.A.X;
            var abY = Post.B.Y - Post.A.Y;
            mTextRot = Math.Atan2(abY, abX);
            var deg = -mTextRot * 180 / Math.PI;
            if (deg < 0.0) {
                deg += 360;
            }
            if (45 * 3 <= deg && deg < 45 * 7) {
                mTextRot += Math.PI;
                interpPost(ref mNamePos, 1 + 0.5 * pw, txtH / Post.Len);
            } else {
                interpPost(ref mNamePos, 1 + 0.5 * pw, -txtH / Post.Len);
            }
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmLabeledNode)Elm;
            drawLeadA();
            drawCenteredText(ce.Text, mNamePos, mTextRot);
            drawPolyline(mTextPoly);
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
            setTextPos();
        }
    }
}
