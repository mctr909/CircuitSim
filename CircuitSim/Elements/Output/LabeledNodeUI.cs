﻿using System.Collections.Generic;
using System.Drawing;

namespace Circuit.Elements.Output {
    class LabeledNodeUI : BaseUI {
        const int FLAG_INTERNAL = 1;
        const int CircleSize = 17;

        Point mPos;

        public LabeledNodeUI(Point pos) : base(pos) {
            Elm = new LabeledNodeElm();
            DumpInfo.ReferenceName = "label";
        }

        public LabeledNodeUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new LabeledNodeElm(st);
        }

        public bool IsInternal { get { return (DumpInfo.Flags & FLAG_INTERNAL) != 0; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.LABELED_NODE; } }

        protected override void dump(List<object> optionList) {
            optionList.Add(((LabeledNodeElm)Elm).Text);
        }

        public override void SetPoints() {
            base.SetPoints();
            var ce = (LabeledNodeElm)Elm;
            if (mPost1.X == mPost2.X) {
                setLead1(1 - 0.5 * Context.GetTextSize(ce.Text).Height / mLen);
            } else {
                setLead1(1 - 0.5 * Context.GetTextSize(ce.Text).Width / mLen);
            }
        }

        public override void Draw(CustomGraphics g) {
            var ce = (LabeledNodeElm)Elm;
            drawLead(mPost1, mLead1);
            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.WhiteColor;
            var str = ce.Text;
            var lineOver = false;
            if (str.StartsWith("/")) {
                lineOver = true;
                str = str.Substring(1);
            }
            drawCenteredText(str, DumpInfo.P2, true);
            if (lineOver) {
                int asc = (int)(CustomGraphics.TextSize + 0.5);
                if (lineOver) {
                    int ya = DumpInfo.P2.Y - asc;
                    int sw = (int)g.GetTextSize(str).Width;
                    g.DrawLine(DumpInfo.P2.X - sw / 2, ya, DumpInfo.P2.X + sw / 2, ya);
                }
            }
            ce.CurCount = updateDotCount(ce.Current, ce.CurCount);
            drawDots(mPost1, mLead1, ce.CurCount);
            interpPoint(ref mPos, 1 + 11.0 / mLen);
            setBbox(mPost1, mPos, CircleSize);
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            var ce = (LabeledNodeElm)Elm;
            arr[0] = ce.Text;
            arr[1] = "I = " + Utils.CurrentText(ce.Current);
            arr[2] = "V = " + Utils.VoltageText(ce.Volts[0]);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (LabeledNodeElm)Elm;
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

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (LabeledNodeElm)Elm;
            if (n == 0) {
                ce.Text = ei.Textf.Text;
            }
            if (n == 1) {
                DumpInfo.Flags = ei.ChangeFlag(DumpInfo.Flags, FLAG_INTERNAL);
            }
        }
    }
}
