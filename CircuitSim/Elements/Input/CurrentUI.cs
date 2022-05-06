using System.Drawing;

namespace Circuit.Elements.Input {
    class CurrentUI : BaseUI {
        const int BODY_LEN = 28;

        Point[] mArrow;
        Point mAshaft1;
        Point mAshaft2;
        Point mCenter;
        Point mTextPos;
        double mCurrentValue;

        public CurrentUI(Point pos) : base(pos) {
            Elm = new CurrentElm();
        }

        public CurrentUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            try {
                Elm = new CurrentElm(st.nextTokenDouble());
            } catch {
            }
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.CURRENT; } }

        protected override string dump() {
            return mCurrentValue.ToString();
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(BODY_LEN);
            interpLead(ref mAshaft1, 0.25);
            interpLead(ref mAshaft2, 0.6);
            interpLead(ref mCenter, 0.5);
            int sign;
            if (mPost1.Y == mPost2.Y) {
                sign = mDsign;
            } else {
                sign = -mDsign;
            }
            interpPoint(ref mTextPos, 0.5, 16 * sign);
            var p2 = new Point();
            interpLead(ref p2, 0.8);
            Utils.CreateArrow(mCenter, p2, out mArrow, 8, 4);
        }

        public override void Draw(CustomGraphics g) {
            draw2Leads();

            g.DrawCircle(mCenter, BODY_LEN / 2);
            drawLead(mAshaft1, mAshaft2);
            g.FillPolygon(NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor, mArrow);

            setBbox(mPost1, mPost2, BODY_LEN);
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

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("電流(A)", mCurrentValue, 0, .1);
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            mCurrentValue = ei.Value;
        }
    }
}
