using System;
using System.Collections.Generic;
using System.Drawing;

namespace Circuit.Elements {
    partial class CircuitElm : Editable {
        const int ColorScaleCount = 64;
        static readonly Pen PenHandle = new Pen(Color.Cyan, 3.0f);

        public static readonly Color SelectColor = Color.Cyan;
        public static Color TextColor { get; set; }
        public static Color WhiteColor { get; set; }
        public static Color GrayColor { get; set; }

        public static double VoltageRange { get; set; } = 5;

        static Color[] mColorScale;

        public static void setColorScale() {
            mColorScale = new Color[ColorScaleCount];
            for (int i = 0; i != ColorScaleCount; i++) {
                double v = i * 2.0 / ColorScaleCount - 1;
                if (v < 0) {
                    int n1 = (int)(128 * -v) + 127;
                    int n2 = (int)(127 * (1 + v));
                    mColorScale[i] = Color.FromArgb(n2, n2, n1);
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

        /// <summary>
        /// draw current dots from point a to b
        /// </summary>
        /// <param name="g"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="pos"></param>
        protected static void drawDots(CustomGraphics g, Point a, Point b, double pos) {
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
            if (Sim.chkPrintableCheckItem.Checked) {
                g.LineColor = GrayColor;
            } else {
                g.LineColor = Color.Yellow;
            }
            for (di = pos; di < dn; di += ds) {
                var x0 = (float)(a.X + di * dx / dn);
                var y0 = (float)(a.Y + di * dy / dn);
                g.FillCircle(x0, y0, 2);
            }
        }

        public void DrawHandles(CustomGraphics g) {
            if (mLastHandleGrabbed == -1) {
                g.FillRectangle(PenHandle.Color, X1 - 3, Y1 - 3, 7, 7);
            } else if (mLastHandleGrabbed == 0) {
                g.FillRectangle(PenHandle.Color, X1 - 4, Y1 - 4, 9, 9);
            }
            if (mNumHandles == 2) {
                if (mLastHandleGrabbed == -1) {
                    g.FillRectangle(PenHandle.Color, X2 - 3, Y2 - 3, 7, 7);
                } else if (mLastHandleGrabbed == 1) {
                    g.FillRectangle(PenHandle.Color, X2 - 4, Y2 - 4, 9, 9);
                }
            }
        }

        protected Color getVoltageColor(double volts) {
            if (NeedsHighlight) {
                return SelectColor;
            }
            if (!Sim.chkVoltsCheckItem.Checked) {
                return WhiteColor;
            }
            if (Sim.chkPrintableCheckItem.Checked) {
                return GrayColor;
            }
            int c = (int)((volts + VoltageRange) * (ColorScaleCount - 1) / (VoltageRange * 2));
            if (c < 0) {
                c = 0;
            }
            if (c >= ColorScaleCount) {
                c = ColorScaleCount - 1;
            }
            return mColorScale[c];
        }

        protected void drawPosts(CustomGraphics g) {
            /* we normally do this in updateCircuit() now because the logic is more complicated.
             * we only handle the case where we have to draw all the posts.  That happens when
             * this element is selected or is being created */
            if (Sim.dragElm == null && !NeedsHighlight) {
                return;
            }
            if (Sim.mouseMode == CirSim.MOUSE_MODE.DRAG_ROW || Sim.mouseMode == CirSim.MOUSE_MODE.DRAG_COLUMN) {
                return;
            }
            for (int i = 0; i != PostCount; i++) {
                var p = GetPost(i);
                g.DrawPost(p);
            }
        }

        protected void draw2Leads(CustomGraphics g) {
            /* draw first lead */
            g.DrawThickLine(getVoltageColor(Volts[0]), mPoint1, mLead1);
            /* draw second lead */
            g.DrawThickLine(getVoltageColor(Volts[1]), mLead2, mPoint2);
        }

        protected void drawCenteredText(CustomGraphics g, string s, int x, int y, bool cx) {
            var fs = g.GetTextSize(s);
            int w = (int)fs.Width;
            int h2 = (int)fs.Height / 2;
            if (cx) {
                adjustBbox(x - w / 2, y - h2, x + w / 2, y + h2);
            } else {
                adjustBbox(x, y - h2, x + w, y + h2);
            }
            g.DrawCenteredText(s, x, y);
        }

        protected void drawCenteredLText(CustomGraphics g, string s, int x, int y, bool cx) {
            var fs = g.GetLTextSize(s);
            int w = (int)fs.Width;
            int h2 = (int)fs.Height / 2;
            if (cx) {
                adjustBbox(x - w / 2, y - h2, x + w / 2, y + h2);
            } else {
                adjustBbox(x, y - h2, x + w, y + h2);
            }
            g.DrawCenteredLText(s, x, y);
        }

        /// <summary>
        /// draw component values (number of resistor ohms, etc).
        /// </summary>
        /// <param name="g"></param>
        /// <param name="s"></param>
        protected void drawValues(CustomGraphics g, string s, int offsetX = 0, int offsetY = 0) {
            if (s == null) {
                return;
            }
            var textSize = g.GetTextSize(s);
            int xc, yc;
            if ((this is RailElm) || (this is SweepElm)) {
                xc = X2;
                yc = Y2;
            } else {
                xc = (X2 + X1) / 2;
                yc = (Y2 + Y1) / 2;
            }
            g.DrawRightText(s, xc + offsetX, yc - textSize.Height + offsetY);
        }

        protected void drawCoil(CustomGraphics g, Point p1, Point p2, double v1, double v2) {
            var coilLen = (float)distance(p1, p2);
            if (0 == coilLen) {
                return;
            }
            /* draw more loops for a longer coil */
            int loopCt = (int)Math.Ceiling(coilLen / 12);
            float w = coilLen / loopCt;
            float h = w * 1.2f;
            float wh = w * 0.5f;
            float hh = h * 0.5f;
            float th = (float)(theta(mLead1, mLead2) * ToDeg);
            var pos = new Point();
            for (int loop = 0; loop != loopCt; loop++) {
                interpPoint(mLead1, mLead2, ref pos, (loop + 0.5) / loopCt, 0);
                double v = v1 + (v2 - v1) * loop / loopCt;
                g.ThickLineColor = getVoltageColor(v);
                g.DrawThickArc(pos.X, pos.Y, w, th, -180);
            }
        }

        protected void drawCoil(CustomGraphics g, float hs, Point p1, Point p2, double v1, double v2) {
            var coilLen = (float)distance(p1, p2);
            if (0 == coilLen) {
                return;
            }
            /* draw more loops for a longer coil */
            int loopCt = (int)Math.Ceiling(coilLen / 12);
            float w = coilLen / loopCt;
            float wh = w * 0.5f;
            hs *= mDsign;
            if (theta(p1, p2) < 0) {
                hs = -hs;
            }
            var pos = new Point();
            for (int loop = 0; loop != loopCt; loop++) {
                interpPoint(p1, p2, ref pos, (loop + 0.5) / loopCt, 0);
                double v = v1 + (v2 - v1) * loop / loopCt;
                g.ThickLineColor = getVoltageColor(v);
                g.DrawThickArc(pos.X, pos.Y, w, hs, -180);
            }
        }

        protected Point[] getSchmittPolygon(float gsize, float ctr) {
            var pts = new Point[6];
            float hs = 3 * gsize;
            float h1 = 3 * gsize;
            float h2 = h1 * 2;
            double len = distance(mLead1, mLead2);
            pts[0] = interpPoint(mLead1, mLead2, ctr - h2 / len, hs);
            pts[1] = interpPoint(mLead1, mLead2, ctr + h1 / len, hs);
            pts[2] = interpPoint(mLead1, mLead2, ctr + h1 / len, -hs);
            pts[3] = interpPoint(mLead1, mLead2, ctr + h2 / len, -hs);
            pts[4] = interpPoint(mLead1, mLead2, ctr - h1 / len, -hs);
            pts[5] = interpPoint(mLead1, mLead2, ctr - h1 / len, hs);
            return pts;
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
            ret.X = (int)Math.Floor(a.X * (1 - f) + b.X * f + 0.48);
            ret.Y = (int)Math.Floor(a.Y * (1 - f) + b.Y * f + 0.48);
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
            ret.X = (int)Math.Floor(a.X * (1 - f) + b.X * f + g * gx + 0.48);
            ret.Y = (int)Math.Floor(a.Y * (1 - f) + b.Y * f + g * gy + 0.48);
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
            ret1.X = (int)(a.X * (1 - f) + b.X * f + g * gx + 0.48);
            ret1.Y = (int)(a.Y * (1 - f) + b.Y * f + g * gy + 0.48);
            ret2.X = (int)(a.X * (1 - f) + b.X * f - g * gx + 0.48);
            ret2.Y = (int)(a.Y * (1 - f) + b.Y * f - g * gy + 0.48);
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
