using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Circuit.Elements {
    partial class CircuitElm : Editable {
        #region CONST
        const int COLOR_SCALE_COUNT = 64;

        static readonly Pen PEN_POST = new Pen(Color.Red, 7.0f);
        static readonly Pen PEN_HANDLE = new Pen(Color.Cyan, 3.0f);

        protected static readonly Font FONT_TERM_NAME = new Font("Meiryo UI", 14.0f);
        protected static readonly Font FONT_TEXT = new Font("Meiryo UI", 9.0f);
        protected static readonly Font FONT_UNITS = new Font("Meiryo UI", 9.0f);
        protected static readonly Brush BRUSH_TERM_NAME = Brushes.White;
        protected static readonly Brush BRUSH_TEXT = Brushes.White;
        protected static readonly StringFormat TEXT_RIGHT = new StringFormat() { Alignment = StringAlignment.Far };
        #endregion

        #region property
        public static double VoltageRange { get; set; } = 5;
        public static Color WhiteColor { get; set; }
        public static Color SelectColor { get; set; }
        public static Color LightGrayColor { get; set; }
        protected static Pen PenLine { get; set; } = new Pen(Color.White, 1.0f) {
            StartCap = LineCap.Triangle,
            EndCap = LineCap.Triangle
        };
        protected static Pen PenThickLine { get; set; } = new Pen(Color.White, 2.0f) {
            StartCap = LineCap.Triangle,
            EndCap = LineCap.Triangle
        };
        #endregion

        static Color[] mColorScale;

        static Pen mPenLine = new Pen(Color.White, 2.0f) {
            StartCap = LineCap.Triangle,
            EndCap = LineCap.Triangle
        };

        public static void setColorScale() {
            mColorScale = new Color[COLOR_SCALE_COUNT];
            for (int i = 0; i != COLOR_SCALE_COUNT; i++) {
                double v = i * 2.0 / COLOR_SCALE_COUNT - 1;
                if (v < 0) {
                    int n1 = (int)(128 * -v) + 127;
                    int n2 = (int)(127 * (1 + v));
                    mColorScale[i] = Color.FromArgb(n1, n2, n2);
                } else {
                    int n1 = (int)(128 * v) + 127;
                    int n2 = (int)(127 * (1 - v));
                    mColorScale[i] = Color.FromArgb(n2, n1, n2);
                }
            }
        }

        protected static Point[] newPointArray(int n) {
            var a = new Point[n];
            while (n > 0) {
                a[--n] = new Point();
            }
            return a;
        }

        public void drawHandles(Graphics g) {
            if (mLastHandleGrabbed == -1) {
                g.FillRectangle(PEN_HANDLE.Brush, X1 - 3, Y1 - 3, 7, 7);
            } else if (mLastHandleGrabbed == 0) {
                g.FillRectangle(PEN_HANDLE.Brush, X1 - 4, Y1 - 4, 9, 9);
            }
            if (mNumHandles == 2) {
                if (mLastHandleGrabbed == -1) {
                    g.FillRectangle(PEN_HANDLE.Brush, X2 - 3, Y2 - 3, 7, 7);
                } else if (mLastHandleGrabbed == 1) {
                    g.FillRectangle(PEN_HANDLE.Brush, X2 - 4, Y2 - 4, 9, 9);
                }
            }
        }

        public static void drawPost(Graphics g, Point p) {
            g.FillPie(PEN_POST.Brush, p.X - PEN_POST.Width / 2, p.Y - PEN_POST.Width / 2, PEN_POST.Width, PEN_POST.Width, 0, 360);
        }

        public static void drawPost(Graphics g, float x, float y) {
            g.FillPie(PEN_POST.Brush, x - PEN_POST.Width / 2, y - PEN_POST.Width / 2, PEN_POST.Width, PEN_POST.Width, 0, 360);
        }

        protected static void drawThickCircle(Graphics g, float centerX, float centerY, int diameter) {
            var md = diameter * .98f;
            g.DrawArc(PenThickLine, centerX - md / 2, centerY - md / 2, md, md, 0, 360);
        }

        protected static void drawLine(Graphics g, float ax, float ay, float bx, float by) {
            g.DrawLine(PenLine, ax, ay, bx, by);
        }

        protected static void drawThickLine(Graphics g, float ax, float ay, float bx, float by) {
            g.DrawLine(PenThickLine, ax, ay, bx, by);
        }

        protected static void drawThickLine(Graphics g, Point a, Point b) {
            g.DrawLine(PenThickLine, a.X, a.Y, b.X, b.Y);
        }

        protected static void drawThickLine(Graphics g, Color c, Point a, Point b) {
            mPenLine.Color = c;
            g.DrawLine(mPenLine, a.X, a.Y, b.X, b.Y);
        }

        protected static void drawThickPolygon(Graphics g, Point[] p) {
            g.DrawPolygon(PenThickLine, p);
        }

        protected static void fillPolygon(Graphics g, Point[] p) {
            g.FillPolygon(PenThickLine.Brush, p);
        }

        protected static void fillPolygon(Graphics g, Color c, Point[] p) {
            mPenLine.Color = c;
            g.FillPolygon(mPenLine.Brush, p);
        }

        protected static List<Point> calcArrow(Point a, Point b, double al, double aw) {
            var poly = new List<Point>();
            poly.Add(new Point(b.X, b.Y));
            var p1 = new Point();
            var p2 = new Point();
            int adx = b.X - a.X;
            int ady = b.Y - a.Y;
            double l = Math.Sqrt(adx * adx + ady * ady);
            interpPoint(a, b, ref p1, ref p2, 1 - al / l, aw);
            poly.Add(p1);
            poly.Add(p2);
            return poly;
        }

        protected static List<Point> createPolygon(params Point[] p) {
            var ret = new List<Point>();
            for (int i = 0; i != p.Length; i++) {
                ret.Add(new Point(p[i].X, p[i].Y));
            }
            return ret;
        }

        protected void drawPosts(Graphics g) {
            /* we normally do this in updateCircuit() now because the logic is more complicated.
             * we only handle the case where we have to draw all the posts.  That happens when
             * this element is selected or is being created */
            if (Sim.dragElm == null && !needsHighlight()) {
                return;
            }
            if (Sim.mouseMode == CirSim.MOUSE_MODE.DRAG_ROW || Sim.mouseMode == CirSim.MOUSE_MODE.DRAG_COLUMN) {
                return;
            }
            for (int i = 0; i != getPostCount(); i++) {
                var p = getPost(i);
                drawPost(g, p);
            }
        }

        protected void draw2Leads(Graphics g) {
            /* draw first lead */
            drawThickLine(g, getVoltageColor(Volts[0]), mPoint1, mLead1);
            /* draw second lead */
            drawThickLine(g, getVoltageColor(Volts[1]), mLead2, mPoint2);
        }

        /// <summary>
        /// draw current dots from point a to b
        /// </summary>
        /// <param name="g"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="pos"></param>
        protected void drawDots(Graphics g, Point a, Point b, double pos) {
            if ((!Sim.simIsRunning()) || pos == 0 || !Sim.chkDotsCheckItem.Checked) {
                return;
            }
            int dx = b.X - a.X;
            int dy = b.Y - a.Y;
            double dn = Math.Sqrt(dx * dx + dy * dy);
            int ds = 16;
            pos %= ds;
            if (pos < 0) {
                pos += ds;
            }
            double di = 0;
            for (di = pos; di < dn; di += ds) {
                var x0 = (float)(a.X + di * dx / dn);
                var y0 = (float)(a.Y + di * dy / dn);
                g.FillRectangle(Brushes.Yellow, x0 - 2, y0 - 2, 4, 4);
            }
        }

        protected void drawCenteredText(Graphics g, string s, int x, int y, bool cx) {
            var fs = g.MeasureString(s, FONT_TERM_NAME);
            int w = (int)fs.Width;
            int h2 = (int)fs.Height / 2;
            if (cx) {
                adjustBbox(x - w / 2, y - h2, x + w / 2, y + h2);
            } else {
                adjustBbox(x, y - h2, x + w, y + h2);
            }

            g.DrawString(s, FONT_TEXT, BRUSH_TEXT, x, y);
        }

        /// <summary>
        /// draw component values (number of resistor ohms, etc).
        /// </summary>
        /// <param name="g"></param>
        /// <param name="s"></param>
        /// <param name="hs">hs = offset</param>
        protected void drawValues(Graphics g, string s, double hs) {
            if (s == null) {
                return;
            }
            var textSize = g.MeasureString(s, FONT_UNITS);
            int ya = (int)textSize.Width;
            int xc, yc;
            if (typeof(RailElm) == GetType() || typeof(SweepElm) == GetType()) {
                xc = X2;
                yc = Y2;
            } else {
                xc = (X2 + X1) / 2;
                yc = (Y2 + Y1) / 2;
            }
            int dpx = (int)(mUnitPx1 * hs);
            int dpy = (int)(mUnitPy1 * hs);
            if (dpx == 0) {
                g.DrawString(s, FONT_UNITS, BRUSH_TEXT, xc - ya / 2, yc - Math.Abs(dpy) - 2 - ya);
            } else {
                int xx = xc + Math.Abs(dpx) + 2;
                if (typeof(VoltageElm) == GetType() || (X1 < X2 && Y1 > Y2)) {
                    xx = xc - (int)(textSize.Width + Math.Abs(dpx) + 2);
                }
                g.DrawString(s, FONT_UNITS, BRUSH_TEXT, xx, yc + dpy);
            }
        }

        protected void drawCoil(Graphics g, int hs, Point p1, Point p2, double v1, double v2) {
            var coilLen = (float)distance(p1, p2);
            if (0 == coilLen) {
                return;
            }
            /* draw more loops for a longer coil */
            int loopCt = (int)Math.Ceiling(coilLen / 12);
            float w = 0.92f * coilLen / loopCt;
            float h = w * 1.2f;
            float wh = w * 0.5f;
            float hh = h * 0.5f;
            float th = (float)(theta(mLead1, mLead2) * TO_DEG);
            for (int loop = 0; loop != loopCt; loop++) {
                interpPoint(mLead1, mLead2, ref ps1, (loop + 0.5) / loopCt, 0);
                double v = v1 + (v2 - v1) * loop / loopCt;
                mPenLine.Color = getVoltageColor(v);
                g.DrawArc(mPenLine, ps1.X - wh, ps1.Y - hh, w, h, th, -180);
            }
        }

        #region Math utils
        public static double theta(Point p1, Point p2) {
            double x = p2.X - p1.X;
            double y = p2.Y - p1.Y;
            return Math.Atan2(y, x);
        }

        public static double distance(Point p1, Point p2) {
            double x = p2.X - p1.X;
            double y = p2.Y - p1.Y;
            return Math.Sqrt(x * x + y * y);
        }

        public static double distance(double x1, double y1, double x2, double y2) {
            double x = x1 - x2;
            double y = y1 - y2;
            return Math.Sqrt(x * x + y * y);
        }

        /// <summary>
        /// calculate point fraction f between a and b, linearly interpolated
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="f"></param>
        /// <returns></returns>
        public static Point interpPoint(Point a, Point b, double f) {
            var p = new Point();
            interpPoint(a, b, ref p, f);
            return p;
        }

        /// <summary>
        /// calculate point fraction f between a and b, linearly interpolated, return it in c
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="ret"></param>
        /// <param name="f"></param>
        public static void interpPoint(Point a, Point b, ref Point ret, double f) {
            ret.X = (int)Math.Floor(a.X * (1 - f) + b.X * f + .48);
            ret.Y = (int)Math.Floor(a.Y * (1 - f) + b.Y * f + .48);
        }

        /// <summary>
        /// Returns a point fraction f along the line between a and b and offset perpendicular by g
        /// </summary>
        /// <param name="a">1st Point</param>
        /// <param name="b">2nd Point</param>
        /// <param name="ret">Returns interpolated point</param>
        /// <param name="f">Fraction along line</param>
        /// <param name="g">Fraction perpendicular to line</param>
        public static void interpPoint(Point a, Point b, ref Point ret, double f, double g) {
            int gx = b.Y - a.Y;
            int gy = a.X - b.X;
            g /= Math.Sqrt(gx * gx + gy * gy);
            ret.X = (int)Math.Floor(a.X * (1 - f) + b.X * f + g * gx + .48);
            ret.Y = (int)Math.Floor(a.Y * (1 - f) + b.Y * f + g * gy + .48);
        }

        /// <summary>
        /// Returns a point fraction f along the line between a and b and offset perpendicular by g
        /// </summary>
        /// <param name="a">1st Point</param>
        /// <param name="b">2nd Point</param>
        /// <param name="f">Fraction along line</param>
        /// <param name="g">Fraction perpendicular to line</param>
        /// <returns>Interpolated point</returns>
        public static Point interpPoint(Point a, Point b, double f, double g) {
            var p = new Point();
            interpPoint(a, b, ref p, f, g);
            return p;
        }

        /// <summary>
        /// Calculates two points fraction f along the line between a and b and offest perpendicular by +/-g
        /// </summary>
        /// <param name="a">1st point (In)</param>
        /// <param name="b">2nd point (In)</param>
        /// <param name="ret1">1st point (Out)</param>
        /// <param name="ret2">2nd point (Out)</param>
        /// <param name="f">Fraction along line</param>
        /// <param name="g">Fraction perpendicular to line</param>
        public static void interpPoint(Point a, Point b, ref Point ret1, ref Point ret2, double f, double g) {
            int gx = b.Y - a.Y;
            int gy = a.X - b.X;
            g /= Math.Sqrt(gx * gx + gy * gy);
            ret1.X = (int)(a.X * (1 - f) + b.X * f + g * gx);
            ret1.Y = (int)(a.Y * (1 - f) + b.Y * f + g * gy);
            ret2.X = (int)(a.X * (1 - f) + b.X * f - g * gx);
            ret2.Y = (int)(a.Y * (1 - f) + b.Y * f - g * gy);
        }
        #endregion

        #region Text Utils
        public static string getVoltageDText(double v) {
            return getUnitText(Math.Abs(v), "V");
        }

        public static string getVoltageText(double v) {
            return getUnitText(v, "V");
        }

        public static string getTimeText(double v) {
            if (v >= 60) {
                double h = Math.Floor(v / 3600);
                v -= 3600 * h;
                double m = Math.Floor(v / 60);
                v -= 60 * m;
                if (h == 0) {
                    return m + ":" + ((v >= 10) ? "" : "0") + v.ToString("0.#");
                }
                return h + ":" + ((m >= 10) ? "" : "0") + m + ":" + ((v >= 10) ? "" : "0") + v.ToString("0.#");
            }
            return getUnitText(v, "s");
        }

        public static string format(double v, bool sf) {
            return sf ? v.ToString("0.#") : v.ToString("0.#");
        }

        public static string getUnitText(double v, string u) {
            return getUnitText(v, u, false);
        }

        public static string getShortUnitText(double v, string u) {
            return getUnitText(v, u, true);
        }

        public static string getUnitText(double v, string u, bool sf) {
            string sp = sf ? "" : " ";
            double va = Math.Abs(v);
            if (va < 1e-14) {
                /* this used to return null, but then wires would display "null" with 0V */
                return "0" + sp + u;
            }
            if (va < 1e-9) {
                return format(v * 1e12, sf) + sp + "p" + u;
            }
            if (va < 1e-6) {
                return format(v * 1e9, sf) + sp + "n" + u;
            }
            if (va < 1e-3) {
                return format(v * 1e6, sf) + sp + CirSim.muString + u;
            }
            if (va < 1) {
                return format(v * 1e3, sf) + sp + "m" + u;
            }
            if (va < 1e3) {
                return format(v, sf) + sp + u;
            }
            if (va < 1e6) {
                return format(v * 1e-3, sf) + sp + "k" + u;
            }
            if (va < 1e9) {
                return format(v * 1e-6, sf) + sp + "M" + u;
            }
            return format(v * 1e-9, sf) + sp + "G" + u;
        }

        public static string getCurrentText(double i) {
            return getUnitText(i, "A");
        }

        public static string getCurrentDText(double i) {
            return getUnitText(Math.Abs(i), "A");
        }

        public static string getUnitTextWithScale(double val, string utext, int scale) {
            if (scale == SCALE_1) {
                return val.ToString("0.000") + " " + utext;
            }
            if (scale == SCALE_M) {
                return (1e3 * val).ToString("0.000") + " m" + utext;
            }
            if (scale == SCALE_MU) {
                return (1e6 * val).ToString("0.000") + " " + CirSim.muString + utext;
            }
            return getUnitText(val, utext);
        }
        #endregion
    }
}
