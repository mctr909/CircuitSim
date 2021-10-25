using System.Drawing;

using Circuit.Elements.Input;
using Circuit.Elements.Custom;

namespace Circuit.Elements.Active {
    class OptocouplerElm : CompositeElm {
        static readonly int[] EXTERNAL_NODES = { 6, 2, 4, 5 };
        static readonly string MODEL_STRING
            = ELEMENTS.DIODE + " 6 1\r"
            + ELEMENTS.CCCS +" 1 2 3 4\r"
            + ELEMENTS.TRANSISTOR_N + " 3 4 5";

        int mCspc;
        int mCspc2;
        Point[] mStubs;
        Point[] mRectPoints;
        Point[] mArrow1;
        Point[] mArrow2;
        double[] mCurCounts;

        DiodeElm mDiode;
        TransistorElm mTransistor;

        public OptocouplerElm(Point pos) : base(pos, MODEL_STRING, EXTERNAL_NODES) {
            mNoDiagonal = true;
            initOptocoupler();
        }

        public OptocouplerElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, null, MODEL_STRING, EXTERNAL_NODES) {
            /* pass st=null since we don't need to undump any of the sub-elements */
            mNoDiagonal = true;
            initOptocoupler();
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.OPTO_COUPLER; } }

        protected override string dump() {
            return dumpWithMask(0);
        }

        public override bool GetConnection(int n1, int n2) {
            return n1 / 2 == n2 / 2;
        }

        void initOptocoupler() {
            mCspc = 8 * 2;
            mCspc2 = mCspc * 2;
            mDiode = (DiodeElm)compElmList[0];
            var cccs = (CCCSElm)compElmList[1];

            cccs.SetExpr(@"max(0,
                min(.0001,
                    select {i-0.003,
                        ( -80000000000*i^5 +800000000*i^4 -3000000*i^3 +5177.20*i^2 +0.2453*i -0.00005 )*1.040/700,
                        (      9000000*i^5    -998113*i^4   +42174*i^3  -861.32*i^2 +9.0836*i -0.00780 )*0.945/700
                    }
                )
            )");

            mTransistor = (TransistorElm)compElmList[2];
            mTransistor.SetHfe(700);
            mCurCounts = new double[4];
            mDiode.ReferenceName = "";
            mTransistor.ReferenceName = "";
        }

        void setPin(int n, int px, int py, double dx, double dy, double dax, double day, int sx, int sy) {
            int pos = n % 2;
            var xa = (int)(px + mCspc2 * dx * pos + sx);
            var ya = (int)(py + mCspc2 * dy * pos + sy);
            setPost(n, new Point((int)(xa + dax * mCspc2), (int)(ya + day * mCspc2)));
            mStubs[n] = new Point((int)(xa + dax * mCspc), (int)(ya + day * mCspc));
        }

        public override void Reset() {
            base.Reset();
            mCurCounts = new double[4];
        }

        public override void SetPoints() {
            base.SetPoints();

            // adapted from ChipElm
            int hs = mCspc;
            int x0 = P1.X + mCspc;
            int y0 = P1.Y;
            var r = new Point(x0 - mCspc, y0 - mCspc / 2);
            int sizeX = 2;
            int sizeY = 2;
            int xs = sizeX * mCspc2;
            int ys = sizeY * mCspc2 - mCspc;
            mRectPoints = new Point[] {
                new Point(r.X, r.Y),
                new Point(r.X + xs, r.Y),
                new Point(r.X + xs, r.Y + ys),
                new Point(r.X, r.Y + ys)
            };
            setBbox(r, mRectPoints[2]);

            mStubs = new Point[4];
            setPin(0, x0, y0, 0, 1, -0.5, 0, 0, 0);
            setPin(1, x0, y0, 0, 1, -0.5, 0, 0, 0);
            setPin(2, x0, y0, 0, 1, 0.5, 0, xs - mCspc2, 0);
            setPin(3, x0, y0, 0, 1, 0.5, 0, xs - mCspc2, 0);

            /* diode */
            mDiode.SetPosition(posts[0].X + 16, posts[0].Y, posts[1].X + 16, posts[1].Y);
            mStubs[0] = mDiode.GetPost(0);
            mStubs[1] = mDiode.GetPost(1);

            /* transistor */
            int midp = (posts[2].Y + posts[3].Y) / 2;
            mTransistor.SetPosition(posts[2].X - 20, midp, posts[2].X - 4, midp);
            mStubs[2] = mTransistor.GetPost(1);
            mStubs[3] = mTransistor.GetPost(2);

            /* create little arrows */
            int sx = mStubs[0].X + 2;
            int sy = (mStubs[0].Y + mStubs[1].Y) / 2;
            int y = sy - 5;
            var p1 = new Point(sx, y);
            var p2 = new Point(sx + 20, y);
            Utils.CreateArrow(p1, p2, out mArrow1, 5, 2);
            y = sy + 5;
            p1 = new Point(sx, y);
            p2 = new Point(sx + 20, y);
            Utils.CreateArrow(p1, p2, out mArrow2, 5, 2);
        }

        public override void Draw(CustomGraphics g) {
            g.LineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            g.DrawPolygon(mRectPoints);

            /* draw stubs */
            for (int i = 0; i != 4; i++) {
                var a = posts[i];
                var b = mStubs[i];
                drawVoltage(i, a, b);
                mCurCounts[i] = updateDotCount(-GetCurrentIntoNode(i), mCurCounts[i]);
                drawDots(a, b, mCurCounts[i]);
            }

            mDiode.Draw(g);
            mTransistor.Draw(g);

            drawPosts();

            /* draw little arrows */
            var c = NeedsHighlight ? CustomGraphics.SelectColor : getVoltageColor(Volts[0]);
            g.FillPolygon(c, mArrow1);
            g.FillPolygon(c, mArrow2);
            g.LineColor = c;
            int sx = mStubs[0].X + 2;
            int sy = (mStubs[0].Y + mStubs[1].Y) / 2;
            for (int i = 0; i != 2; i++) {
                int y = sy + i * 10 - 5;
                g.DrawLine(sx + 10, y, sx + 15, y);
            }
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "optocoupler";
            arr[1] = "Iin = " + Utils.CurrentText(GetCurrentIntoNode(0));
            arr[2] = "Iout = " + Utils.CurrentText(GetCurrentIntoNode(2));
        }
    }
}
