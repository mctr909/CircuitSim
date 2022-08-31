﻿using System.Collections.Generic;
using System.Drawing;

namespace Circuit.Elements.Output {
    class AmmeterUI : BaseUI {
        const int FLAG_SHOWCURRENT = 1;

        Point mMid;
        Point[] mArrowPoly;
        Point mTextPos;

        public AmmeterUI(Point pos) : base(pos) {
            Elm = new AmmeterElm();
            DumpInfo.Flags = FLAG_SHOWCURRENT;
        }

        public AmmeterUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new AmmeterElm(st);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.AMMETER; } }

        protected override void dump(List<object> optionList) {
            var ce = (AmmeterElm)Elm;
            optionList.Add(ce.Meter);
            optionList.Add(ce.Scale);
        }

        public override void SetPoints() {
            base.SetPoints();
            interpPoint(ref mMid, 0.5 + 4 / mLen);
            Utils.CreateArrow(mPost1, mMid, out mArrowPoly, 9, 5);
            mNameV = mPost1.X == mPost2.X;
            if (mNameV) {
                interpPoint(ref mTextPos, 0.5, -21 * mDsign);
            } else {
                interpPoint(ref mTextPos, 0.5, 12 * mDsign);
            }
        }

        public override void Draw(CustomGraphics g) {
            base.Draw(g); /* BC required for highlighting */
            var ce = (AmmeterElm)Elm;

            drawLead(mPost1, mPost2);
            g.FillPolygon(NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor, mArrowPoly);
            doDots();
            setBbox(mPost1, mPost2, 3);
            string s = "A";
            switch (ce.Meter) {
            case AmmeterElm.AM_VOL:
                s = Utils.UnitTextWithScale(ce.Current, "A", ce.Scale);
                break;
            case AmmeterElm.AM_RMS:
                s = Utils.UnitTextWithScale(ce.RmsI, "A(rms)", ce.Scale);
                break;
            }
            if (mNameV) {
                g.DrawCenteredVText(s, mTextPos.X, mTextPos.Y);
            } else {
                g.DrawCenteredText(s, mTextPos.X, mTextPos.Y);
            }
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            var ce = (AmmeterElm)Elm;
            arr[0] = "Ammeter";
            switch (ce.Meter) {
            case AmmeterElm.AM_VOL:
                arr[1] = "I = " + Utils.UnitText(ce.Current, "A");
                break;
            case AmmeterElm.AM_RMS:
                arr[1] = "Irms = " + Utils.UnitText(ce.RmsI, "A");
                break;
            }
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            var ce = (AmmeterElm)Elm;
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

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (AmmeterElm)Elm;
            if (n == 0) {
                ce.Meter = ei.Choice.SelectedIndex;
            }
            if (n == 1) {
                ce.Scale = (E_SCALE)ei.Choice.SelectedIndex;
            }
        }
    }
}
