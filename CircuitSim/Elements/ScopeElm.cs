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
            SetPoints();
        }

        public ScopeElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
            mNoDiagonal = false;
            string sStr = st.nextToken();
            var sst = new StringTokenizer(sStr, "_");
            elmScope = new Scope(Sim);
            elmScope.undump(sst);
            SetPoints();
            elmScope.resetGraph();
        }

        public override bool CanViewInScope { get { return false; } }

        public override int PostCount { get { return 0; } }

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

        public override void SetPoints() {
            base.SetPoints();
            setScopeRect();
        }

        public void setElmScope(Scope s) {
            elmScope = s;
        }

        public void stepScope() {
            elmScope.timeStep();
        }

        public override void Reset() {
            base.Reset();
            elmScope.resetGraph(true);
        }

        public void clearElmScope() {
            elmScope = null;
        }

        public override void Draw(Graphics g) {
            var color = needsHighlight() ? SelectColor : WhiteColor;
            setScopeRect();
            elmScope.draw(g);
            setBbox(mPoint1, mPoint2, 0);
            drawPosts(g);
        }
    }
}
