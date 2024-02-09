using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements.Input;

namespace Circuit.Symbol.Input {
    class Current : BaseSymbol {
        const int BODY_LEN = 24;

        PointF[] mArrow;
        PointF mAshaft1;
        PointF mAshaft2;
        PointF mCenter;
        PointF mTextPos;
        double mCurrentValue;
        ElmCurrent mElm;

        public override BaseElement Element { get { return mElm; } }

        public Current(Point pos) : base(pos) {
            mElm = new ElmCurrent();
        }

        public Current(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            mElm = new ElmCurrent(st.nextTokenDouble());
        }

        public override DUMP_ID DumpId { get { return DUMP_ID.CURRENT; } }

        protected override void dump(List<object> optionList) {
            optionList.Add(mCurrentValue);
        }

        public override void SetPoints() {
            base.SetPoints();
            setLeads(BODY_LEN);
            interpLead(ref mAshaft1, 0.25);
            interpLead(ref mAshaft2, 0.6);
            interpLead(ref mCenter, 0.5);
            int sign;
            if (Post.Horizontal) {
                sign = Post.Dsign;
            } else {
                sign = -Post.Dsign;
            }
            interpPost(ref mTextPos, 0.5, 30 * sign);
            var p2 = new PointF();
            interpLead(ref p2, 0.8);
            Utils.CreateArrow(mCenter, p2, out mArrow, 8, 4);
        }

        public override void Draw(CustomGraphics g) {
            draw2Leads();
            drawCircle(mCenter, BODY_LEN / 2);
            drawLine(mAshaft1, mAshaft2);
            fillPolygon(mArrow);
            doDots();
            if (ControlPanel.ChkShowValues.Checked) {
                var s = Utils.CurrentText(mCurrentValue);
                drawCenteredText(s, mTextPos);
            }
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
