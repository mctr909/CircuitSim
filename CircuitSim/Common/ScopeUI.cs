using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Elements;
using Circuit.Elements.Output;

using Circuit.UI;

namespace Circuit {
    class ScopeUI : BaseUI {
        public Scope Scope;

        public ScopeUI(Point pos) : base(pos) {
            mNoDiagonal = false;
            DumpInfo.SetP2(DumpInfo.P1.X + 128, DumpInfo.P1.Y + 64);
            Scope = new Scope();
            Elm = new ScopeElm(Scope);
            SetPoints();
        }

        public ScopeUI(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            mNoDiagonal = false;
            string sStr = st.nextToken();
            var sst = new StringTokenizer(sStr, "\t");
            Scope = new Scope();
            Elm = new ScopeElm(Scope);
            Scope.Undump(sst);
            SetPoints();
            Scope.ResetGraph();
        }

        public override bool CanViewInScope { get { return false; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.SCOPE; } }

        protected override void dump(List<object> optionList) {
            string sStr = Scope.Dump().Replace(' ', '\t');
            sStr = sStr.Replace("o\t", ""); /* remove unused prefix for embedded Scope */
            optionList.Add(sStr);
        }

        public void setScopeElm(BaseUI e) {
            Scope.SetElm(e);
            Scope.ResetGraph();
        }

        public void setScopeRect() {
            int i1 = CirSimForm.TransformX(Math.Min(DumpInfo.P1.X, DumpInfo.P2.X));
            int i2 = CirSimForm.TransformX(Math.Max(DumpInfo.P1.X, DumpInfo.P2.X));
            int j1 = CirSimForm.TransformY(Math.Min(DumpInfo.P1.Y, DumpInfo.P2.Y));
            int j2 = CirSimForm.TransformY(Math.Max(DumpInfo.P1.Y, DumpInfo.P2.Y));
            var r = new Rectangle(i1, j1, i2 - i1, j2 - j1);
            if (!r.Equals(Scope.BoundingBox)) {
                Scope.SetRect(r);
            }
        }

        public override void SetPoints() {
            base.SetPoints();
            setScopeRect();
        }

        public void setElmScope(Scope s) {
            Scope = s;
        }

        public void stepScope() {
            Scope.TimeStep();
        }

        public void clearElmScope() {
            Scope = null;
        }

        public override void Draw(CustomGraphics g) {
            setScopeRect();
            setBbox(mPost1, mPost2, 0);
            Scope.Draw(g, true);
            drawPosts();
        }
    }
}
