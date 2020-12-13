using System;
using System.Drawing;

namespace Circuit.Elements {
    class TriStateElm : CircuitElm {
        double resistance;
        double r_on;
        double r_off;
        bool open;

        Point ps;
        Point point3;
        Point lead3;
        Point[] gatePoly;

        public TriStateElm(int xx, int yy) : base(xx, yy) {
            r_on = 0.1;
            r_off = 1e10;
        }

        public TriStateElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            r_on = 0.1;
            r_off = 1e10;
            try {
                r_on = st.nextTokenDouble();
                r_off = st.nextTokenDouble();
            } catch (Exception e) {
            }
        }

        /* we need this to be able to change the matrix for each step */
        public override bool NonLinear {
            get { return true; }
        }

        public override int PostCount { get { return 3; } }

        public override int InternalNodeCount { get { return 1; } }

        public override int VoltageSourceCount { get { return 1; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.TRISTATE; } }

        protected override string dump() {
            return r_on + " " + r_off;
        }

        public override void SetPoints() {
            base.SetPoints();
            calcLeads(32);
            ps = new Point();
            int hs = 16;
            int ww = 16;
            if (ww > mLen / 2) {
                ww = (int)(mLen / 2);
            }
            gatePoly = new Point[3];
            Utils.InterpPoint(mLead1, mLead2, ref gatePoly[0], ref gatePoly[1], 0, hs);
            gatePoly[2] = Utils.InterpPoint(mPoint1, mPoint2, .5 + ww / mLen);
            point3 = Utils.InterpPoint(mPoint1, mPoint2, .5, -hs);
            lead3 = Utils.InterpPoint(mPoint1, mPoint2, .5, -hs / 2);
        }

        public override void Draw(CustomGraphics g) {
            int hs = 16;
            setBbox(mPoint1, mPoint2, hs);

            draw2Leads(g);

            g.DrawThickPolygon(NeedsHighlight ? SelectColor : GrayColor, gatePoly);
            g.DrawThickLine(getVoltageColor(Volts[2]), point3, lead3);
            mCurCount = updateDotCount(mCurrent, mCurCount);
            drawDots(g, mLead2, mPoint2, mCurCount);
            drawPosts(g);
        }

        public override double GetCurrentIntoNode(int n) {
            if (n == 1) {
                return mCurrent;
            }
            return 0;
        }

        protected override void calculateCurrent() {
            mCurrent = (Volts[0] - Volts[1]) / resistance;
        }

        public override void Stamp() {
            mCir.StampVoltageSource(0, Nodes[3], mVoltSource);
            mCir.StampNonLinear(Nodes[3]);
            mCir.StampNonLinear(Nodes[1]);
        }

        public override void DoStep() {
            open = Volts[2] < 2.5;
            resistance = open ? r_off : r_on;
            mCir.StampResistor(Nodes[3], Nodes[1], resistance);
            mCir.UpdateVoltageSource(0, Nodes[3], mVoltSource, Volts[0] > 2.5 ? 5 : 0);
        }

        public override void Drag(int xx, int yy) {
            xx = Sim.snapGrid(xx);
            yy = Sim.snapGrid(yy);
            if (Math.Abs(X1 - xx) < Math.Abs(Y1 - yy)) {
                xx = X1;
            } else {
                yy = Y1;
            }
            int q1 = Math.Abs(X1 - xx) + Math.Abs(Y1 - yy);
            int q2 = (q1 / 2) % Sim.gridSize;
            if (q2 != 0) {
                return;
            }
            X2 = xx;
            Y2 = yy;
            SetPoints();
        }

        public override Point GetPost(int n) {
            return (n == 0) ? mPoint1 : (n == 1) ? mPoint2 : point3;
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "tri-state buffer";
            arr[1] = open ? "open" : "closed";
            arr[2] = "Vd = " + Utils.VoltageDText(VoltageDiff);
            arr[3] = "I = " + Utils.CurrentDText(Current);
            arr[4] = "Vc = " + Utils.VoltageText(Volts[2]);
        }

        /* there is no current path through the input, but there
         * is an indirect path through the output to ground. */
        public override bool GetConnection(int n1, int n2) {
            return false;
        }

        public override bool HasGroundConnection(int n1) {
            return n1 == 1;
        }

        public override EditInfo GetEditInfo(int n) {
            if (n == 0) {
                return new EditInfo("On Resistance (ohms)", r_on, 0, 0);
            }
            if (n == 1) {
                return new EditInfo("Off Resistance (ohms)", r_off, 0, 0);
            }
            return null;
        }

        public override void SetEditValue(int n, EditInfo ei) {
            if (n == 0 && ei.Value > 0) {
                r_on = ei.Value;
            }
            if (n == 1 && ei.Value > 0) {
                r_off = ei.Value;
            }
        }
    }
}
