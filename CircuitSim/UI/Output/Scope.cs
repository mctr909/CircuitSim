using System;
using System.Collections.Generic;
using System.Drawing;

using Circuit.Common;
using Circuit.Elements.Output;

namespace Circuit.UI.Output {
    class Scope : BaseUI {
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

        public override void SetPoints() {
            base.SetPoints();
            var a = MouseInfo.ToScreenPos(Post.A);
            var b = MouseInfo.ToScreenPos(Post.B);
            int x1 = Math.Min(a.X, b.X);
            int x2 = Math.Max(a.X, b.X);
            int y1 = Math.Min(a.Y, b.Y);
            int y2 = Math.Max(a.Y, b.Y);
            var r = new Rectangle(x1, y1, x2 - x1, y2 - y1);
            if (!r.Equals(Plot.BoundingBox)) {
                Plot.SetRect(r);
            }
        }

        public override void Draw(CustomGraphics g) {
            Plot.MouseCursorX = MouseInfo.Cursor.X;
            Plot.MouseCursorY = MouseInfo.Cursor.Y;
            Plot.Draw(g, true);
        }
    }
}