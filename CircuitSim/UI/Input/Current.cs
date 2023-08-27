using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Input;

namespace Circuit.UI.Input {
    class Current : BaseUI {
        const int BODY_LEN = 28;

        PointF[] mArrow;
        PointF mAshaft1;
        PointF mAshaft2;
        PointF mCenter;
        Point mTextPos;
        double mCurrentValue;

        public Current(Point pos) : base(pos) {
            Elm = new ElmCurrent();
        }

        public Current(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new ElmCurrent(st.nextTokenDouble());
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
            if (Post.Horizontal) {
                sign = Post.Dsign;
            } else {
                sign = -Post.Dsign;
            }
            interpPost(ref mTextPos, 0.5, 16 * sign);
            var p2 = new PointF();
            interpLead(ref p2, 0.8);
            Utils.CreateArrow(mCenter, p2, out mArrow, 8, 4);
        }

        public override void Draw(CustomGraphics g) {
            draw2Leads();

            drawCircle(mCenter, BODY_LEN / 2);
            drawLine(mAshaft1, mAshaft2);
            fillPolygon(mArrow);

            setBbox(BODY_LEN);
            doDots();
            if (ControlPanel.ChkShowValues.Checked) {
                string s = Utils.UnitText(mCurrentValue, "A");
                g.DrawRightText(s, mTextPos.X, mTextPos.Y);
            }
            drawPosts();
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "電流源";
            getBasicInfo(1, arr);
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
