using System.Drawing;

using Circuit.Elements.Input;
using Circuit.Elements.Custom;

namespace Circuit.Elements.Active {
    class OptocouplerElm : CompositeElm {
        private static string modelString
            = ELEMENTS.DIODE + " 6 1\r"
            + ELEMENTS.CCCSElm +" 1 2 3 4\r"
            + ELEMENTS.TRANSISTOR_N + " 3 4 5";
        private static int[] modelExternalNodes = { 6, 2, 4, 5 };

        int csize;
        int cspc;
        int cspc2;
        Point[] stubs;
        Point[] rectPoints;
        double[] curCounts;

        DiodeElm diode;
        TransistorElm transistor;

        public OptocouplerElm(Point pos) : base(pos, modelString, modelExternalNodes) {
            mNoDiagonal = true;
            initOptocoupler();
        }

        public OptocouplerElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, null, modelString, modelExternalNodes) {
            /* pass st=null since we don't need to undump any of the sub-elements */
            mNoDiagonal = true;
            initOptocoupler();
        }

        public override DUMP_ID DumpType { get { return DUMP_ID.OPTO_COUPLER; } }

        protected override string dump() {
            return dumpWithMask(0);
        }

        private void initOptocoupler() {
            csize = 2;
            cspc = 8 * 2;
            cspc2 = cspc * 2;
            diode = (DiodeElm)compElmList[0];
            var cccs = (CCCSElm)compElmList[1];

            cccs.setExpr(@"max(0,
                min(.0001,
                    select {i-0.003,
                        ( -80000000000*i^5 +800000000*i^4 -3000000*i^3 +5177.20*i^2 +0.2453*i -0.00005 )*1.040/700,
                        (      9000000*i^5    -998113*i^4   +42174*i^3  -861.32*i^2 +9.0836*i -0.00780 )*0.945/700
                    }
                )
            )");

            transistor = (TransistorElm)compElmList[2];
            transistor.setBeta(700);
            curCounts = new double[4];
        }

        public override void Reset() {
            base.Reset();
            curCounts = new double[4];
        }

        public override bool GetConnection(int n1, int n2) {
            return n1 / 2 == n2 / 2;
        }

        public override void Draw(CustomGraphics g) {
            g.ThickLineColor = NeedsHighlight ? CustomGraphics.SelectColor : CustomGraphics.GrayColor;
            g.DrawThickPolygon(rectPoints);

            /* draw stubs */
            for (int i = 0; i != 4; i++) {
                var a = posts[i];
                var b = stubs[i];
                drawVoltage(g, i, a, b);
                curCounts[i] = updateDotCount(-GetCurrentIntoNode(i), curCounts[i]);
                drawDots(g, a, b, curCounts[i]);
            }

            diode.Draw(g);
            transistor.Draw(g);

            drawPosts(g);

            /* draw little arrows */
            var c = NeedsHighlight ? CustomGraphics.SelectColor : getVoltageColor(Volts[0]);
            g.LineColor = c;
            int sx = stubs[0].X + 2;
            int sy = (stubs[0].Y + stubs[1].Y) / 2;
            for (int i = 0; i != 2; i++) {
                int y = sy + i * 10 - 5;
                var p1 = new Point(sx, y);
                var p2 = new Point(sx + 20, y);
                Point[] p;
                Utils.CreateArrow(p1, p2, out p, 5, 2);
                g.FillPolygon(c, p);
                g.DrawLine(sx + 10, y, sx + 15, y);
            }
        }

        public override void SetPoints() {
            base.SetPoints();

            // adapted from ChipElm
            int hs = cspc;
            int x0 = P1.X + cspc2;
            int y0 = P1.Y;
            int xr = x0 - cspc;
            int yr = y0 - cspc / 2;
            int sizeX = 2;
            int sizeY = 2;
            int xs = sizeX * cspc2;
            int ys = sizeY * cspc2 - cspc;
            rectPoints = new Point[] {
                new Point(xr, yr),
                new Point(xr + xs, yr),
                new Point(xr + xs, yr + ys),
                new Point(xr, yr + ys)
            };
            setBbox(xr, yr, rectPoints[2].X, rectPoints[2].Y);

            stubs = new Point[4];
            //        setPin(0, x0, y0, 1, 0, 0, -1, 0, 0);
            //        setPin(1, x0, y0, 1, 0, 0,  1, 0, ys-cspc2);
            //        setPin(2, x0, y0, 1, 0, 0, -1, 0, 0);
            //        setPin(3, x0, y0, 1, 0, 0,  1, 0, ys-cspc2);
            setPin(0, x0, y0, 0, 1, -1, 0, 0, 0);
            setPin(1, x0, y0, 0, 1, -1, 0, 0, 0);
            setPin(2, x0, y0, 0, 1, 1, 0, xs - cspc2, 0);
            setPin(3, x0, y0, 0, 1, 1, 0, xs - cspc2, 0);
            diode.SetPosition(posts[0].X + 32, posts[0].Y, posts[1].X + 32, posts[1].Y);
            stubs[0] = diode.GetPost(0);
            stubs[1] = diode.GetPost(1);

            int midp = (posts[2].Y + posts[3].Y) / 2;
            transistor.SetPosition(posts[2].X - 40, midp, posts[2].X - 24, midp);
            stubs[2] = transistor.GetPost(1);
            stubs[3] = transistor.GetPost(2);
        }

        void setPin(int n, int px, int py, int dx, int dy, int dax, int day, int sx, int sy) {
            int pos = n % 2;
            //		(n < 2) ? 0 : 1;
            int xa = px + cspc2 * dx * pos + sx;
            int ya = py + cspc2 * dy * pos + sy;
            setPost(n, new Point(xa + dax * cspc2, ya + day * cspc2));
            stubs[n] = new Point(xa + dax * cspc, ya + day * cspc);
        }

        public override void GetInfo(string[] arr) {
            arr[0] = "optocoupler";
            arr[1] = "Iin = " + Utils.CurrentText(GetCurrentIntoNode(0));
            arr[2] = "Iout = " + Utils.CurrentText(GetCurrentIntoNode(2));
        }
    }
}
