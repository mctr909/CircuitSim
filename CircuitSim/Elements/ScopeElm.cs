using System;
using System.Drawing;

namespace Circuit.Elements {
    class ScopeElm : CircuitElm {
        public Scope elmScope;

        public ScopeElm(int xx, int yy) : base(xx, yy) {
            mNoDiagonal = false;
            X2 = X1 + 128;
            Y2 = Y1 + 64;
            elmScope = new Scope(Sim);
            setPoints();
        }

        public ScopeElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            mNoDiagonal = false;
            string sStr = st.nextToken();
            var sst = new StringTokenizer(sStr, "_");
            elmScope = new Scope(Sim);
            elmScope.undump(sst);
            setPoints();
            elmScope.resetGraph();
        }

        protected override string dump() {
            string sStr = elmScope.dump().Replace(' ', '_');
            sStr = sStr.Replace("o_", ""); /* remove unused prefix for embedded Scope */
            return sStr;
        }

        protected override DUMP_ID getDumpType() { return DUMP_ID.SCOPE; }

        public void setScopeElm(CircuitElm e) {
            elmScope.setElm(e);
            elmScope.resetGraph();
        }

        public void setScopeRect() {
            int i1 = Sim.transformX(Math.Min(X1, X2));
            int i2 = Sim.transformX(Math.Max(X1, X2));
            int j1 = Sim.transformY(Math.Min(Y1, Y2));
            int j2 = Sim.transformY(Math.Max(Y1, Y2));
            var r = new Rectangle(i1, j1, i2 - i1, j2 - j1);
            if (!r.Equals(elmScope.BoundingBox)) {
                elmScope.setRect(r);
            }
        }

        public override void setPoints() {
            base.setPoints();
            setScopeRect();
        }

        public void setElmScope(Scope s) {
            elmScope = s;
        }

        public void stepScope() {
            elmScope.timeStep();
        }

        public override void reset() {
            base.reset();
            elmScope.resetGraph(true);
        }

        public void clearElmScope() {
            elmScope = null;
        }

        public override bool canViewInScope() { return false; }

        public override void draw(Graphics g) {
            var color = needsHighlight() ? SelectColor : WhiteColor;
            setScopeRect();
            elmScope.draw(g);
            setBbox(mPoint1, mPoint2, 0);
            drawPosts(g);
        }

        public override int getPostCount() { return 0; }
    }
}
