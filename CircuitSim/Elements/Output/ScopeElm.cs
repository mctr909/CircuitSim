using System;
using System.Drawing;

namespace Circuit.Elements.Output {
    class ScopeElm : CircuitElm {
        public Scope elmScope;

        public ScopeElm(Point pos) : base(pos) {
            mNoDiagonal = false;
            P2.X = P1.X + 128;
            P2.Y = P1.Y + 64;
            elmScope = new Scope();
            SetPoints();
        }

        public ScopeElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            mNoDiagonal = false;
            string sStr = st.nextToken();
            var sst = new StringTokenizer(sStr, "_");
            elmScope = new Scope();
            elmScope.Undump(sst);
            SetPoints();
            elmScope.ResetGraph();
        }

        public override bool CanViewInScope { get { return false; } }

        public override int PostCount { get { return 0; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.SCOPE; } }

        protected override string dump() {
            string sStr = elmScope.Dump().Replace(' ', '_');
            sStr = sStr.Replace("o_", ""); /* remove unused prefix for embedded Scope */
            return sStr;
        }

        public void setScopeElm(CircuitElm e) {
            elmScope.SetElm(e);
            elmScope.ResetGraph();
        }

        public void setScopeRect() {
            int i1 = CirSim.Sim.TransformX(Math.Min(P1.X, P2.X));
            int i2 = CirSim.Sim.TransformX(Math.Max(P1.X, P2.X));
            int j1 = CirSim.Sim.TransformY(Math.Min(P1.Y, P2.Y));
            int j2 = CirSim.Sim.TransformY(Math.Max(P1.Y, P2.Y));
            var r = new Rectangle(i1, j1, i2 - i1, j2 - j1);
            if (!r.Equals(elmScope.BoundingBox)) {
                elmScope.SetRect(r);
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
            elmScope.TimeStep();
        }

        public override void Reset() {
            base.Reset();
            elmScope.ResetGraph(true);
        }

        public void clearElmScope() {
            elmScope = null;
        }

        public override void Draw(CustomGraphics g) {
            var color = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.WhiteColor;
            setScopeRect();
            elmScope.Draw(g);
            setBbox(mPoint1, mPoint2, 0);
            drawPosts(g);
        }
    }
}
