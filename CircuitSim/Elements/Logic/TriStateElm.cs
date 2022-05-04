using System;
using System.Drawing;

namespace Circuit.Elements.Logic {
    class TriStateElm : CircuitElm {
        const int BODY_LEN = 32;

        double resistance;
        double r_on;
        double r_off;
        bool open;

        Point point3;
        Point lead3;
        Point[] gatePoly;

        public TriStateElm(Point pos) : base(pos) {
            r_on = 0.1;
            r_off = 1e10;
        }

        public TriStateElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            r_on = 0.1;
            r_off = 1e10;
            try {
                r_on = st.nextTokenDouble();
                r_off = st.nextTokenDouble();
            } catch { }
        }

        /* we need this to be able to change the matrix for each step */
        public override bool CirNonLinear {
            get { return true; }
        }

        public override int CirPostCount { get { return 3; } }

        public override int CirInternalNodeCount { get { return 1; } }

        public override int CirVoltageSourceCount { get { return 1; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.TRISTATE; } }

        protected override string dump() {
            return r_on + " " + r_off;
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(BODY_LEN);
            int hs = BODY_LEN / 2;
            int ww = BODY_LEN / 2;
            if (ww > mLen / 2) {
                ww = (int)(mLen / 2);
            }
            gatePoly = new Point[3];
            interpLeadAB(ref gatePoly[0], ref gatePoly[1], 0, hs);
            interpPoint(ref gatePoly[2], 0.5 + ww / mLen);
            interpPoint(ref point3, 0.5, -hs);
            interpPoint(ref lead3, 0.5, -hs / 2);
        }

        public override void Draw(CustomGraphics g) {
            int hs = 16;
            setBbox(mPoint1, mPoint2, hs);

            draw2Leads();

            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            g.DrawPolygon(gatePoly);
            drawLead(point3, lead3);
            mCirCurCount = cirUpdateDotCount(mCirCurrent, mCirCurCount);
            drawDots(mLead2, mPoint2, mCirCurCount);
            drawPosts();
        }

        public override double CirGetCurrentIntoNode(int n) {
            if (n == 1) {
                return mCirCurrent;
            }
            return 0;
        }

        protected override void cirCalculateCurrent() {
            mCirCurrent = (CirVolts[0] - CirVolts[1]) / resistance;
        }

        public override void CirStamp() {
            mCir.StampVoltageSource(0, CirNodes[3], mCirVoltSource);
            mCir.StampNonLinear(CirNodes[3]);
            mCir.StampNonLinear(CirNodes[1]);
        }

        public override void CirDoStep() {
            open = CirVolts[2] < 2.5;
            resistance = open ? r_off : r_on;
            mCir.StampResistor(CirNodes[3], CirNodes[1], resistance);
            mCir.UpdateVoltageSource(0, CirNodes[3], mCirVoltSource, CirVolts[0] > 2.5 ? 5 : 0);
        }

        public override void Drag(Point pos) {
            pos = CirSim.Sim.SnapGrid(pos);
            if (Math.Abs(P1.X - pos.X) < Math.Abs(P1.Y - pos.Y)) {
                pos.X = P1.X;
            } else {
                pos.Y = P1.Y;
            }
            int q1 = Math.Abs(P1.X - pos.X) + Math.Abs(P1.Y - pos.Y);
            int q2 = (q1 / 2) % CirSim.GRID_SIZE;
            if (q2 != 0) {
                return;
            }
            P2.X = pos.X;
            P2.Y = pos.Y;
            SetPoints();
        }

        public override Point GetPost(int n) {
            return (n == 0) ? mPoint1 : (n == 1) ? mPoint2 : point3;
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "tri-state buffer";
            arr[1] = open ? "open" : "closed";
            arr[2] = "Vd = " + Utils.VoltageAbsText(CirVoltageDiff);
            arr[3] = "I = " + Utils.CurrentAbsText(CirCurrent);
            arr[4] = "Vc = " + Utils.VoltageText(CirVolts[2]);
        }

        /* there is no current path through the input, but there
         * is an indirect path through the output to ground. */
        public override bool CirGetConnection(int n1, int n2) {
            return false;
        }

        public override bool CirHasGroundConnection(int n1) {
            return n1 == 1;
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                return new ElementInfo("On Resistance (ohms)", r_on, 0, 0);
            }
            if (n == 1) {
                return new ElementInfo("Off Resistance (ohms)", r_off, 0, 0);
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0 && ei.Value > 0) {
                r_on = ei.Value;
            }
            if (n == 1 && ei.Value > 0) {
                r_off = ei.Value;
            }
        }
    }
}
