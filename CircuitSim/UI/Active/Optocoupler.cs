using System.Drawing;

using Circuit.Elements.Active;
using Circuit.UI.Custom;

namespace Circuit.UI.Active {
    class Optocoupler : Composite {
        Point[] mStubs;
        PointF[] mRectPoints;
        PointF[] mArrow1;
        PointF[] mArrow2;

        public Optocoupler(Point pos) : base(pos) {
            Elm = new ElmOptocoupler();
            var ce = (ElmOptocoupler)Elm;
            ce.Posts = new Point[((ElmOptocoupler)Elm).NumPosts];
            Post.NoDiagonal = true;
        }

        public Optocoupler(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f) {
            Elm = new ElmOptocoupler(st);
            var ce = (ElmOptocoupler)Elm;
            ce.Posts = new Point[((ElmOptocoupler)Elm).NumPosts];
            /* pass st=null since we don't need to undump any of the sub-elements */
            Post.NoDiagonal = true;
        }

        public override DUMP_ID DumpId { get { return DUMP_ID.OPTO_COUPLER; } }

        void setPin(int n, int px, int py, double dx, double dy, double dax, double day, int sx, int sy) {
            var ce = (ElmOptocoupler)Elm;
            int pos = n % 2;
            var xa = (int)(px + ce.mCspc2 * dx * pos + sx);
            var ya = (int)(py + ce.mCspc2 * dy * pos + sy);
            mStubs[n] = new Point((int)(xa + dax * ce.mCspc2), (int)(ya + day * ce.mCspc2));
            setPost(n, mStubs[n]);
        }

        public override void SetPoints() {
            base.SetPoints();
            var ce = (ElmOptocoupler)Elm;

            // adapted from ChipElm
            int x0 = Post.A.X + ce.mCspc;
            int y0 = Post.A.Y;
            var r = new Point(x0 - ce.mCspc, y0 - ce.mCspc / 2);
            var sizeX = 1.5f;
            int sizeY = 2;
            int xs = (int)(sizeX * ce.mCspc2);
            int ys = sizeY * ce.mCspc2 - ce.mCspc - 3;
            mRectPoints = new PointF[] {
                new Point(r.X, r.Y + 3),
                new Point(r.X + xs, r.Y + 3),
                new Point(r.X + xs, r.Y + ys),
                new Point(r.X, r.Y + ys)
            };
            Post.SetBbox(r, mRectPoints[2]);

            mStubs = new Point[4];
            setPin(0, x0, y0, 0, 1, -0.5, 0, 0, 0);
            setPin(1, x0, y0, 0, 1, -0.5, 0, 0, 0);
            setPin(2, x0, y0, 0, 1, 0.5, 0, xs - ce.mCspc2, 0);
            setPin(3, x0, y0, 0, 1, 0.5, 0, xs - ce.mCspc2, 0);
            Post.B = mStubs[2];

            /* diode */
            ce.mDiode.SetPosition(ce.Posts[0].X + 10, ce.Posts[0].Y, ce.Posts[1].X + 10, ce.Posts[1].Y);
            mStubs[0] = ce.mDiode.Elm.GetNodePos(0);
            mStubs[1] = ce.mDiode.Elm.GetNodePos(1);

            /* transistor */
            int midp = (ce.Posts[2].Y + ce.Posts[3].Y) / 2;
            ce.mTransistor.SetPosition(ce.Posts[2].X - 18, midp, ce.Posts[2].X - 6, midp);
            mStubs[2] = ce.mTransistor.Elm.GetNodePos(1);
            mStubs[3] = ce.mTransistor.Elm.GetNodePos(2);

            /* create little arrows */
            int sx1 = mStubs[0].X;
            int sx2 = sx1 + 17;
            int sy = (mStubs[0].Y + mStubs[1].Y) / 2;
            int y = sy - 5;
            var p1 = new Point(sx1, y);
            var p2 = new Point(sx2, y);
            Utils.CreateArrow(p1, p2, out mArrow1, 5, 3);
            y = sy + 5;
            p1 = new Point(sx1, y);
            p2 = new Point(sx2, y);
            Utils.CreateArrow(p1, p2, out mArrow2, 5, 3);
        }

        public override void Draw(CustomGraphics g) {
            var ce = (ElmOptocoupler)Elm;

            drawPolygon(mRectPoints);

            /* draw stubs */
            for (int i = 0; i != 4; i++) {
                var a = ce.Posts[i];
                var b = mStubs[i];
                drawLine(a, b);
            }

            ce.mDiode.Draw(g);
            ce.mTransistor.Draw(g);

            /* draw little arrows */
            var sx1 = mArrow1[0].X - 10;
            var sx2 = sx1 + 5;
            drawLine(sx1, mArrow1[0].Y, sx2, mArrow1[0].Y);
            drawLine(sx1, mArrow2[0].Y, sx2, mArrow2[0].Y);
            fillPolygon(mArrow1);
            fillPolygon(mArrow2);
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "optocoupler";
        }
    }
}
