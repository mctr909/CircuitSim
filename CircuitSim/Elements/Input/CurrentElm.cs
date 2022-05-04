using System.Drawing;

namespace Circuit.Elements.Input {
    class CurrentElm : CircuitElm {
        const int BODY_LEN = 28;

        Point[] mArrow;
        Point mAshaft1;
        Point mAshaft2;
        Point mCenter;
        Point mTextPos;
        double mCurrentValue;

        public CurrentElm(Point pos) : base(pos) {
            mCurrentValue = 0.01;
        }

        public CurrentElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            try {
                mCurrentValue = st.nextTokenDouble();
            } catch {
                mCurrentValue = 0.01;
            }
        }

        public override double CirVoltageDiff { get { return CirVolts[1] - CirVolts[0]; } }

        public override double CirPower { get { return -CirVoltageDiff * mCirCurrent; } }

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
            if (mPoint1.Y == mPoint2.Y) {
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

            setBbox(mPoint1, mPoint2, BODY_LEN);
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

        /* we defer stamping current sources until we can tell if they have a current path or not */
        public void stampCurrentSource(bool broken) {
            if (broken) {
                /* no current path; stamping a current source would cause a matrix error. */
                mCir.StampResistor(CirNodes[0], CirNodes[1], 1e8);
                mCirCurrent = 0;
            } else {
                /* ok to stamp a current source */
                mCir.StampCurrentSource(CirNodes[0], CirNodes[1], mCurrentValue);
                mCirCurrent = mCurrentValue;
            }
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
