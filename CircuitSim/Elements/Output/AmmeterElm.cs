using System.Drawing;
using System.Windows.Forms;

namespace Circuit.Elements.Output {
    class AmmeterElm : CircuitElm {
        const int FLAG_SHOWCURRENT = 1;

        Point mMid;
        Point[] mArrowPoly;
        Point mTextPos;

        public AmmeterElm(Point pos) : base(pos) {
            CirElm = new AmmeterElmE();
            mFlags = FLAG_SHOWCURRENT;
        }

        public AmmeterElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            CirElm = new AmmeterElmE(st);
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.AMMETER; } }

        protected override string dump() {
            var ce = (AmmeterElmE)CirElm;
            return ce.Meter + " " + ce.Scale;
        }

        public override void SetPoints() {
            base.SetPoints();
            interpPoint(ref mMid, 0.5 + 4 / mLen);
            Utils.CreateArrow(mPoint1, mMid, out mArrowPoly, 9, 5);
            int sign;
            mNameV = mPoint1.X == mPoint2.X;
            if (mNameV) {
                sign = -mDsign;
                if (mPoint1.Y < mPoint2.Y) {
                    interpPoint(ref mTextPos, (mLen - 5) / mLen, 21 * sign);
                } else {
                    interpPoint(ref mTextPos, 5 / mLen, 21 * sign);
                }
            } else {
                sign = mDsign;
                if(mPoint1.X < mPoint2.X) {
                    interpPoint(ref mTextPos, (mLen - 5) / mLen, 12 * sign);
                } else {
                    interpPoint(ref mTextPos, 5 / mLen, 12 * sign);
                }
            }
        }

        public override void Draw(CustomGraphics g) {
            base.Draw(g); /* BC required for highlighting */
            var ce = (AmmeterElmE)CirElm;

            drawLead(mPoint1, mPoint2);
            g.FillPolygon(NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor, mArrowPoly);
            doDots();
            setBbox(mPoint1, mPoint2, 3);
            string s = "A";
            switch (ce.Meter) {
            case AmmeterElmE.AM_VOL:
                s = Utils.UnitTextWithScale(ce.mCurrent, "A", ce.Scale);
                break;
            case AmmeterElmE.AM_RMS:
                s = Utils.UnitTextWithScale(ce.RmsI, "A(rms)", ce.Scale);
                break;
            }
            if (mNameV) {
                g.DrawRightVText(s, mTextPos.X, mTextPos.Y);
            } else {
                g.DrawRightText(s, mTextPos.X, mTextPos.Y);
            }
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            var ce = (AmmeterElmE)CirElm;
            arr[0] = "Ammeter";
            switch (ce.Meter) {
            case AmmeterElmE.AM_VOL:
                arr[1] = "I = " + Utils.UnitText(ce.mCurrent, "A");
                break;
            case AmmeterElmE.AM_RMS:
                arr[1] = "Irms = " + Utils.UnitText(ce.RmsI, "A");
                break;
            }
        }

        public override ElementInfo GetElementInfo(int n) {
            var ce = (AmmeterElmE)CirElm;
            if (n == 0) {
                var ei = new ElementInfo("表示", ce.SelectedValue, -1, -1);
                ei.Choice = new ComboBox();
                ei.Choice.Items.Add("瞬時値");
                ei.Choice.Items.Add("実効値");
                ei.Choice.SelectedIndex = ce.Meter;
                return ei;
            }
            if (n == 1) {
                var ei = new ElementInfo("スケール", 0);
                ei.Choice = new ComboBox();
                ei.Choice.Items.Add("自動");
                ei.Choice.Items.Add("A");
                ei.Choice.Items.Add("mA");
                ei.Choice.Items.Add("uA");
                ei.Choice.SelectedIndex = (int)ce.Scale;
                return ei;
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            var ce = (AmmeterElmE)CirElm;
            if (n == 0) {
                ce.Meter = ei.Choice.SelectedIndex;
            }
            if (n == 1) {
                ce.Scale = (E_SCALE)ei.Choice.SelectedIndex;
            }
        }

        bool mustShowCurrent() {
            return (mFlags & FLAG_SHOWCURRENT) != 0;
        }
    }
}
