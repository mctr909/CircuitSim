using System.Drawing;

namespace Circuit.InputElements {
    class CurrentElm : CircuitElm {
        Point[] arrow;
        Point ashaft1;
        Point ashaft2;
        Point center;
        Point textPos;
        double currentValue;

        public CurrentElm(Point pos) : base(pos) {
            currentValue = .01;
        }

        public CurrentElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            try {
                currentValue = st.nextTokenDouble();
            } catch {
                currentValue = .01;
            }
        }

        public override double VoltageDiff { get { return Volts[1] - Volts[0]; } }

        public override double Power { get { return -VoltageDiff * mCurrent; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.CURRENT; } }

        protected override string dump() {
            return currentValue.ToString();
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(32);
            Utils.InterpPoint(mLead1, mLead2, ref ashaft1, .25);
            Utils.InterpPoint(mLead1, mLead2, ref ashaft2, .6);
            Utils.InterpPoint(mLead1, mLead2, ref center, .5);
            int sign;
            if (mPoint1.Y == mPoint2.Y) {
                sign = mDsign;
            } else {
                sign = -mDsign;
            }
            Utils.InterpPoint(mPoint1, mPoint2, ref textPos, 0.5, 20 * sign);
            var p2 = new Point();
            Utils.InterpPoint(mLead1, mLead2, ref p2, .8);
            Utils.CreateArrow(center, p2, out arrow, 8, 4);
        }

        public override void Draw(CustomGraphics g) {
            int cr = 32;
            draw2Leads(g);

            var c = getVoltageColor((Volts[0] + Volts[1]) / 2);
            g.ThickLineColor = c;
            g.DrawThickCircle(center, cr);
            g.DrawThickLine(ashaft1, ashaft2);
            g.FillPolygon(c, arrow);

            setBbox(mPoint1, mPoint2, cr);
            doDots(g);
            if (ControlPanel.ChkShowValues.Checked) {
                string s = Utils.ShortUnitText(currentValue, "A");
                g.DrawRightText(s, textPos.X, textPos.Y);
            }
            drawPosts(g);
        }

        /* we defer stamping current sources until we can tell if they have a current path or not */
        public void stampCurrentSource(bool broken) {
            if (broken) {
                /* no current path; stamping a current source would cause a matrix error. */
                mCir.StampResistor(Nodes[0], Nodes[1], 1e8);
                mCurrent = 0;
            } else {
                /* ok to stamp a current source */
                mCir.StampCurrentSource(Nodes[0], Nodes[1], currentValue);
                mCurrent = currentValue;
            }
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("Current (A)", currentValue, 0, .1);
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            currentValue = ei.Value;
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "current source";
            getBasicInfo(arr);
        }
    }
}
