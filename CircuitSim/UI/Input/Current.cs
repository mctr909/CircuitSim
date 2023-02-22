using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Input;

namespace Circuit.UI.Input {
    class Current : BaseUI {
        const int BODY_LEN = 28;

        Point[] mArrow;
        Point mAshaft1;
        Point mAshaft2;
        Point mCenter;
        Point mTextPos;
        double mCurrentValue;

        public Current(Point pos) : base(pos) {
            Elm = new ElmCurrent();
        }

        public Current(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            try {
                Elm = new ElmCurrent(st.nextTokenDouble());
            } catch {
            }
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.CURRENT; } }

        protected override void dump(List<object> optionList) {
            optionList.Add(mCurrentValue);
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(BODY_LEN);
            interpLead(ref mAshaft1, 0.25);
            interpLead(ref mAshaft2, 0.6);
            interpLead(ref mCenter, 0.5);
            int sign;
            if (mHorizontal) {
                sign = mDsign;
            } else {
                sign = -mDsign;
            }
            interpPoint(ref mTextPos, 0.5, 16 * sign);
            var p2 = new Point();
            interpLead(ref p2, 0.8);
            Utils.CreateArrow(mCenter.X, mCenter.Y, p2.X, p2.Y, out mArrow, 8, 4);
        }

        public override void Draw(CustomGraphics g) {
            draw2Leads();

            g.DrawCircle(mCenter, BODY_LEN / 2);
            drawLead(mAshaft1, mAshaft2);
            g.FillPolygon(NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor, mArrow);

            setBbox(BODY_LEN);
            doDots();
            if (ControlPanel.ChkShowValues.Checked) {
                string s = Utils.UnitText(mCurrentValue, "A");
                g.DrawRightText(s, mTextPos.X, mTextPos.Y);
            }
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "current source";
            getBasicInfo(arr);
        }

        public override ElementInfo GetElementInfo(int r, int c) {
            if (c != 0) {
                return null;
            }
            if (r == 0) {
                return new ElementInfo("電流(A)", mCurrentValue);
            }
            return null;
        }

        public override void SetElementValue(int n, int c, ElementInfo ei) {
            mCurrentValue = ei.Value;
        }
    }
}
