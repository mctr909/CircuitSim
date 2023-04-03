using System.Drawing;

using Circuit.Elements.Active;
using Circuit.UI.Custom;

namespace Circuit.UI.Active {
    class Optocoupler : Composite {
        Point[] mStubs;
        Point[] mRectPoints;
        Point[] mArrow1;
        Point[] mArrow2;

        public Optocoupler(Point pos) : base(pos) {
            Elm = new ElmOptocoupler();
            var ce = (ElmOptocoupler)Elm;
            ce.Posts = new Point[((ElmOptocoupler)Elm).NumPosts];
            mNoDiagonal = true;
        }

        public Optocoupler(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new ElmOptocoupler(st);
            var ce = (ElmOptocoupler)Elm;
            ce.Posts = new Point[((ElmOptocoupler)Elm).NumPosts];
            /* pass st=null since we don't need to undump any of the sub-elements */
            mNoDiagonal = true;
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.OPTO_COUPLER; } }

        void setPin(int n, int px, int py, double dx, double dy, double dax, double day, int sx, int sy) {
            var ce = (ElmOptocoupler)Elm;
            int pos = n % 2;
            var xa = (int)(px + ce.mCspc2 * dx * pos + sx);
            var ya = (int)(py + ce.mCspc2 * dy * pos + sy);
            setPost(n, new Point((int)(xa + dax * ce.mCspc2), (int)(ya + day * ce.mCspc2)));
            mStubs[n] = new Point((int)(xa + dax * ce.mCspc), (int)(ya + day * ce.mCspc));
        }

        public override void SetPoints() {
            base.SetPoints();
            var ce = (ElmOptocoupler)Elm;

            // adapted from ChipElm
            int hs = ce.mCspc;
            int x0 = DumpInfo.P1.X + ce.mCspc;
            int y0 = DumpInfo.P1.Y;
            var r = new Point(x0 - ce.mCspc, y0 - ce.mCspc / 2);
            int sizeX = 2;
            int sizeY = 2;
            int xs = sizeX * ce.mCspc2;
            int ys = sizeY * ce.mCspc2 - ce.mCspc;
            mRectPoints = new Point[] {
                new Point(r.X, r.Y),
                new Point(r.X + xs, r.Y),
                new Point(r.X + xs, r.Y + ys),
                new Point(r.X, r.Y + ys)
            };
            DumpInfo.SetBbox(r, mRectPoints[2]);

            mStubs = new Point[4];
            setPin(0, x0, y0, 0, 1, -0.5, 0, 0, 0);
            setPin(1, x0, y0, 0, 1, -0.5, 0, 0, 0);
            setPin(2, x0, y0, 0, 1, 0.5, 0, xs - ce.mCspc2, 0);
            setPin(3, x0, y0, 0, 1, 0.5, 0, xs - ce.mCspc2, 0);

            /* diode */
            ce.mDiode.SetPosition(ce.Posts[0].X + 16, ce.Posts[0].Y, ce.Posts[1].X + 16, ce.Posts[1].Y);
            mStubs[0] = ce.mDiode.Elm.GetPost(0);
            mStubs[1] = ce.mDiode.Elm.GetPost(1);

            /* transistor */
            int midp = (ce.Posts[2].Y + ce.Posts[3].Y) / 2;
            ce.mTransistor.SetPosition(ce.Posts[2].X - 20, midp, ce.Posts[2].X - 4, midp);
            mStubs[2] = ce.mTransistor.Elm.GetPost(1);
            mStubs[3] = ce.mTransistor.Elm.GetPost(2);

            /* create little arrows */
            int sx = mStubs[0].X + 2;
            int sy = (mStubs[0].Y + mStubs[1].Y) / 2;
            int y = sy - 5;
            var p1 = new Point(sx, y);
            var p2 = new Point(sx + 20, y);
            Utils.CreateArrow(p1.X, p1.Y, p2.X, p2.Y, out mArrow1, 5, 2);
            y = sy + 5;
            p1 = new Point(sx, y);
            p2 = new Point(sx + 20, y);
            Utils.CreateArrow(p1.X, p1.Y, p2.X, p2.Y, out mArrow2, 5, 2);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmOptocoupler)Elm;
            g.DrawColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            g.DrawPolygon(mRectPoints);

            /* draw stubs */
            for (int i = 0; i != 4; i++) {
                var a = ce.Posts[i];
                var b = mStubs[i];
                drawLead(a, b);
            }

            ce.mDiode.Draw(g);
            ce.mTransistor.Draw(g);

            drawPosts();

            /* draw little arrows */
            var c = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.LineColor;
            g.FillPolygon(c, mArrow1);
            g.FillPolygon(c, mArrow2);
            g.DrawColor = c;
            int sx = mStubs[0].X + 2;
            int sy = (mStubs[0].Y + mStubs[1].Y) / 2;
            for (int i = 0; i != 2; i++) {
                int y = sy + i * 10 - 5;
                g.DrawLine(sx + 10, y, sx + 15, y);
            }
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "optocoupler";
        }
    }
}
