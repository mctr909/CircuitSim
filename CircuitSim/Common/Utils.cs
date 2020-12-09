using System;
using System.Drawing;

namespace Circuit {
    enum E_SCALE {
        AUTO,
        X1,
        M,
        MU,
    }

    static class Utils {
        public static double Angle(Point o, Point p) {
            double x = p.X - o.X;
            double y = p.Y - o.Y;
            return Math.Atan2(y, x);
        }

        public static double Angle(Point o, double px, double py) {
            double x = px - o.X;
            double y = py - o.Y;
            return Math.Atan2(y, x);
        }

        public static double Angle(double ox, double oy, Point p) {
            double x = p.X - ox;
            double y = p.Y - oy;
            return Math.Atan2(y, x);
        }

        public static double Angle(double ox, double oy, double px, double py) {
            double x = px - ox;
            double y = py - oy;
            return Math.Atan2(y, x);
        }

        public static double Distance(Point a, Point b) {
            double x = b.X - a.X;
            double y = b.Y - a.Y;
            return Math.Sqrt(x * x + y * y);
        }

        public static double Distance(Point a, double bx, double by) {
            double x = bx - a.X;
            double y = by - a.Y;
            return Math.Sqrt(x * x + y * y);
        }

        public static double Distance(double ax, double ay, double bx, double by) {
            double x = bx - ax;
            double y = by - ay;
            return Math.Sqrt(x * x + y * y);
        }

        /// <summary>
        /// calculate point fraction f between a and b, linearly interpolated
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="f"></param>
        /// <returns></returns>
        public static Point InterpPoint(Point a, Point b, double f) {
            var p = new Point();
            InterpPoint(a, b, ref p, f);
            return p;
        }

        /// <summary>
        /// calculate point fraction f between a and b, linearly interpolated, return it in c
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="ret"></param>
        /// <param name="f"></param>
        public static void InterpPoint(Point a, Point b, ref Point ret, double f) {
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
        public static void InterpPoint(Point a, Point b, ref Point ret, double f, double g) {
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
        /// <param name="ret">Returns interpolated point</param>
        /// <param name="f">Fraction along line</param>
        /// <param name="g">Fraction perpendicular to line</param>
        public static void InterpPoint(Point a, Point b, ref PointF ret, double f, double g) {
            int gx = b.Y - a.Y;
            int gy = a.X - b.X;
            g /= Math.Sqrt(gx * gx + gy * gy);
            ret.X = (float)Math.Floor(a.X * (1 - f) + b.X * f + g * gx);
            ret.Y = (float)Math.Floor(a.Y * (1 - f) + b.Y * f + g * gy);
        }

        /// <summary>
        /// Returns a point fraction f along the line between a and b and offset perpendicular by g
        /// </summary>
        /// <param name="a">1st Point</param>
        /// <param name="b">2nd Point</param>
        /// <param name="f">Fraction along line</param>
        /// <param name="g">Fraction perpendicular to line</param>
        /// <returns>Interpolated point</returns>
        public static Point InterpPoint(Point a, Point b, double f, double g) {
            var p = new Point();
            InterpPoint(a, b, ref p, f, g);
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
        public static void InterpPoint(Point a, Point b, ref Point ret1, ref Point ret2, double f, double g) {
            int gx = b.Y - a.Y;
            int gy = a.X - b.X;
            g /= Math.Sqrt(gx * gx + gy * gy);
            ret1.X = (int)Math.Floor(a.X * (1 - f) + b.X * f + g * gx + 0.48);
            ret1.Y = (int)Math.Floor(a.Y * (1 - f) + b.Y * f + g * gy + 0.48);
            ret2.X = (int)Math.Floor(a.X * (1 - f) + b.X * f - g * gx + 0.48);
            ret2.Y = (int)Math.Floor(a.Y * (1 - f) + b.Y * f - g * gy + 0.48);
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
        public static void InterpPoint(Point a, Point b, ref PointF ret1, ref PointF ret2, double f, double g) {
            int gx = b.Y - a.Y;
            int gy = a.X - b.X;
            g /= Math.Sqrt(gx * gx + gy * gy);
            ret1.X = (float)Math.Floor(a.X * (1 - f) + b.X * f + g * gx);
            ret1.Y = (float)Math.Floor(a.Y * (1 - f) + b.Y * f + g * gy);
            ret2.X = (float)Math.Floor(a.X * (1 - f) + b.X * f - g * gx);
            ret2.Y = (float)Math.Floor(a.Y * (1 - f) + b.Y * f - g * gy);
        }

        public static Point[] CreateArrow(Point a, Point b, double al, double aw) {
            int adx = b.X - a.X;
            int ady = b.Y - a.Y;
            double l = Math.Sqrt(adx * adx + ady * ady);
            var poly = new Point[3];
            poly[0] = new Point(b.X, b.Y);
            InterpPoint(a, b, ref poly[1], ref poly[2], 1.0 - al / l, aw);
            return poly;
        }

        public static string VoltageText(double v) {
            return unitText(v, "V", false);
        }

        public static string VoltageDText(double v) {
            return unitText(Math.Abs(v), "V", false);
        }

        public static string CurrentText(double i) {
            return unitText(i, "A", false);
        }

        public static string CurrentDText(double i) {
            return unitText(Math.Abs(i), "A", false);
        }

        public static string UnitText(double v, string u) {
            return unitText(v, u, false);
        }

        public static string ShortUnitText(double v, string u) {
            return unitText(v, u, true);
        }

        public static string TimeText(double v) {
            if (v >= 60) {
                double h = Math.Floor(v / 3600);
                v -= 3600 * h;
                double m = Math.Floor(v / 60);
                v -= 60 * m;
                if (h == 0) {
                    return m + ":" + ((v >= 10) ? "" : "0") + v.ToString("0.00");
                }
                return h + ":" + ((m >= 10) ? "" : "0") + m + ":" + ((v >= 10) ? "" : "0") + v.ToString("0.00");
            }
            return unitText(v, "s", false);
        }

        public static string UnitTextWithScale(double val, string utext, E_SCALE scale) {
            if (scale == E_SCALE.X1) {
                return val.ToString("0.000") + " " + utext;
            }
            if (scale == E_SCALE.M) {
                return (1e3 * val).ToString("0.000") + " m" + utext;
            }
            if (scale == E_SCALE.MU) {
                return (1e6 * val).ToString("0.000") + " " + CirSim.muString + utext;
            }
            return unitText(val, utext, false);
        }

        static string unitText(double v, string u, bool sf) {
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

        static string format(double v, bool sf) {
            return sf ? v.ToString("0.##") : v.ToString("0.00");
        }
    }
}
