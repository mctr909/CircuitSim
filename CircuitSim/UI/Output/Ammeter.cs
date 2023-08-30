using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Output;

namespace Circuit.UI.Output {
    class Ammeter : BaseUI {
        const int FLAG_SHOWCURRENT = 1;

        PointF mMid;
        PointF[] mArrowPoly;
        Point mTextPos;

        public Ammeter(Point pos) : base(pos) {
            Elm = new ElmAmmeter();
            mFlags = FLAG_SHOWCURRENT;
        }

        public Ammeter(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new ElmAmmeter(st);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.AMMETER; } }

        protected override void dump(List<object> optionList) {
            var ce = (ElmAmmeter)Elm;
            optionList.Add(ce.Meter);
            optionList.Add(ce.Scale);
        }

        public override void SetPoints() {
            base.SetPoints();
            Post.SetBbox(0);
            interpPost(ref mMid, 0.5 + 4 / Post.Len);
            Utils.CreateArrow(Elm.Post[0], mMid, out mArrowPoly, 9, 5);
            if (Post.Vertical) {
                interpPost(ref mTextPos, 0.5, -21 * Post.Dsign);
            } else {
                interpPost(ref mTextPos, 0.5, 12 * Post.Dsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            base.Draw(g); /* BC required for highlighting */
            var ce = (ElmAmmeter)Elm;

            drawLine(Elm.Post[0], Elm.Post[1]);
            fillPolygon(mArrowPoly);
            doDots();

            string s = "A";
            switch (ce.Meter) {
            case ElmAmmeter.AM_VOL:
                s = Utils.UnitTextWithScale(ce.Current, "A", ce.Scale);
                break;
            case ElmAmmeter.AM_RMS:
                s = Utils.UnitTextWithScale(ce.RmsI, "A(rms)", ce.Scale);
                break;
            }
            if (Post.Vertical) {
                drawCenteredText(s, mTextPos, -Math.PI / 2);
            } else {
                drawCenteredText(s, mTextPos);
            }
        }

        public override void GetInfo(string[] arr) {
            var ce = (ElmAmmeter)Elm;
            arr[0] = "電流計";
            switch (ce.Meter) {
            case ElmAmmeter.AM_VOL:
                arr[1] = "電流：" + Utils.CurrentText(ce.Current);
                break;
            case ElmAmmeter.AM_RMS:
                arr[1] = "電流(rms)：" + Utils.CurrentText(ce.RmsI);
                break;
            }
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (ElmAmmeter)Elm;
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("表示", ce.Meter, new string[] { "瞬時値", "実効値" });
            }
            if (r == 1) {
                return new ElementInfo("スケール", (int)ce.Scale, new string[] { "自動", "A", "mA", "uA" });
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            var ce = (ElmAmmeter)Elm;
            if (n == 0) {
                ce.Meter = ei.Choice.SelectedIndex;
            }
            if (n == 1) {
                ce.Scale = (E_SCALE)ei.Choice.SelectedIndex;
            }
        }
    }
}
