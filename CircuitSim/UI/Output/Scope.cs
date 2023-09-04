using System;
using System.Collections.Generic;
using System.Drawing;
using Circuit.Common;
using Circuit.Elements.Output;

namespace Circuit.UI.Output {
    public class Scope : BaseUI {
        public ScopePlot Plot;

        public Scope(Point pos) : base(pos) {
            Post.B.X = Post.A.X + 128;
            Post.B.Y = Post.A.Y + 64;
            Plot = new ScopePlot();
            Elm = new ElmScope(Plot);
            SetPoints();
        }

        public Scope(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            string sStr;
            st.nextToken(out sStr);
            var sst = new StringTokenizer(sStr, "\t");
            Plot = new ScopePlot();
            Elm = new ElmScope(Plot);
            Plot.Undump(sst);
            SetPoints();
            Plot.ResetGraph();
        }

        public override bool CanViewInScope { get { return false; } }

        public override DUMP_ID DumpId { get { return DUMP_ID.SCOPE; } }

        protected override void dump(List<object> optionList) {
            string sStr = Plot.Dump().Replace(' ', '\t');
            sStr = sStr.Replace("o\t", ""); /* remove unused prefix for embedded Scope */
            optionList.Add(sStr);
        }

        public void SetScopeUI(BaseUI ui) {
            Plot.SetUI(ui);
            Plot.ResetGraph();
        }

        public void StepScope() {
            Plot.TimeStep();
        }

        public override void SetPoints() {
            base.SetPoints();
            int x1 = CirSimForm.TransformX(Math.Min(Post.A.X, Post.B.X));
            int x2 = CirSimForm.TransformX(Math.Max(Post.A.X, Post.B.X));
            int y1 = CirSimForm.TransformY(Math.Min(Post.A.Y, Post.B.Y));
            int y2 = CirSimForm.TransformY(Math.Max(Post.A.Y, Post.B.Y));
            var r = new Rectangle(x1, y1, x2 - x1, y2 - y1);
            if (!r.Equals(Plot.BoundingBox)) {
                Plot.SetRect(r);
            }
        }

        public override void Draw(CustomGraphics g) {
            Plot.MouseCursorX = CirSimForm.MouseCursorX;
            Plot.MouseCursorY = CirSimForm.MouseCursorY;
            Plot.Draw(g, true);
        }
    }
}