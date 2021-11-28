﻿using System;
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
            var x = p.X - o.X;
            var y = p.Y - o.Y;
            return Math.Atan2(y, x);
        }

        public static double Angle(Point o, double px, double py) {
            var x = px - o.X;
            var y = py - o.Y;
            return Math.Atan2(y, x);
        }

        public static double Angle(double ox, double oy, Point p) {
            var x = p.X - ox;
            var y = p.Y - oy;
            return Math.Atan2(y, x);
        }

        public static double Angle(double ox, double oy, double px, double py) {
            var x = px - ox;
            var y = py - oy;
            return Math.Atan2(y, x);
        }

        public static double Distance(Point a, Point b) {
            var x = b.X - a.X;
            var y = b.Y - a.Y;
            return Math.Sqrt(x * x + y * y);
        }

        public static double Distance(Point a, double bx, double by) {
            var x = bx - a.X;
            var y = by - a.Y;
            return Math.Sqrt(x * x + y * y);
        }

        public static double Distance(double ax, double ay, double bx, double by) {
            var x = bx - ax;
            var y = by - ay;
            return Math.Sqrt(x * x + y * y);
        }

        public static double DistanceOnLine(double ax, double ay, double bx, double by, double px, double py) {
            var abx = bx - ax;
            var aby = by - ay;
            var apx = px - ax;
            var apy = py - ay;
            var r = (apx * abx + apy * aby) / (abx * abx + aby * aby);
            if (1.0 < r) {
                r = 1.0;
            }
            if (r < 0.0) {
                r = 0.0;
            }
            var sx = px - (ax + abx * r);
            var sy = py - (ay + aby * r);
            return Math.Sqrt(sx * sx + sy * sy);
        }

        /// <summary>
        /// calculate point fraction f between a and b, linearly interpolated, return it in c
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="ret"></param>
        /// <param name="f"></param>
        public static void InterpPoint(Point a, Point b, ref Point ret, double f) {
            ret.X = (int)Math.Floor(a.X * (1 - f) + b.X * f + 0.45);
            ret.Y = (int)Math.Floor(a.Y * (1 - f) + b.Y * f + 0.45);
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
            var gx = b.Y - a.Y;
            var gy = a.X - b.X;
            var r = Math.Sqrt(gx * gx + gy * gy);
            if (0.0 == r) {
                ret.X = a.X;
                ret.Y = a.Y;
            } else {
                g /= r;
                ret.X = (int)Math.Floor(a.X * (1 - f) + b.X * f + g * gx + 0.45);
                ret.Y = (int)Math.Floor(a.Y * (1 - f) + b.Y * f + g * gy + 0.45);
            }
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
            var gx = b.Y - a.Y;
            var gy = a.X - b.X;
            var r = Math.Sqrt(gx * gx + gy * gy);
            if (0.0 == r) {
                ret1.X = a.X;
                ret1.Y = a.Y;
                ret2.X = b.X;
                ret2.Y = b.Y;
            } else {
                g /= r;
                ret1.X = (int)Math.Floor(a.X * (1 - f) + b.X * f + g * gx + 0.45);
                ret1.Y = (int)Math.Floor(a.Y * (1 - f) + b.Y * f + g * gy + 0.45);
                ret2.X = (int)Math.Floor(a.X * (1 - f) + b.X * f - g * gx + 0.45);
                ret2.Y = (int)Math.Floor(a.Y * (1 - f) + b.Y * f - g * gy + 0.45);
            }
        }

        public static void CreateArrow(Point a, Point b, out Point[] ret, double al, double aw) {
            var adx = b.X - a.X;
            var ady = b.Y - a.Y;
            var l = Math.Sqrt(adx * adx + ady * ady);
            ret = new Point[3];
            ret[0] = new Point(b.X, b.Y);
            InterpPoint(a, b, ref ret[1], ref ret[2], 1.0 - al / l, aw);
        }

        public static void CreateSchmitt(Point a, Point b, out Point[] ret, double gsize, double ctr) {
            ret = new Point[6];
            var hs = 3 * gsize;
            var h1 = 3 * gsize;
            var h2 = h1 * 2;
            var len = Distance(a, b);
            InterpPoint(a, b, ref ret[0], ctr - h2 / len, hs);
            InterpPoint(a, b, ref ret[1], ctr + h1 / len, hs);
            InterpPoint(a, b, ref ret[2], ctr + h1 / len, -hs);
            InterpPoint(a, b, ref ret[3], ctr + h2 / len, -hs);
            InterpPoint(a, b, ref ret[4], ctr - h1 / len, -hs);
            InterpPoint(a, b, ref ret[5], ctr - h1 / len, hs);
        }

        public static string VoltageText(double v) {
            return unitText(v, "V");
        }

        public static string VoltageAbsText(double v) {
            return unitText(Math.Abs(v), "V");
        }

        public static string CurrentText(double i) {
            return unitText(i, "A");
        }

        public static string CurrentAbsText(double i) {
            return unitText(Math.Abs(i), "A");
        }

        public static string UnitText(double v, string u = "") {
            return unitText(v, u);
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
            return unitText(v, "s");
        }

        public static string UnitTextWithScale(double val, string utext, E_SCALE scale) {
            if (scale == E_SCALE.X1) {
                return val.ToString("0.00") + " " + utext;
            }
            if (scale == E_SCALE.M) {
                return (1e3 * val).ToString("0") + " m" + utext;
            }
            if (scale == E_SCALE.MU) {
                return (1e6 * val).ToString("0") + " u" + utext;
            }
            return unitText(val, utext, false);
        }

        public static bool TextToNum(string text, out double num) {
            text = text.Replace(" ", "");
            if (0 <= text.IndexOf("p")) {
                var ret = double.TryParse(text.Replace("p", ""), out num);
                num *= 1e-12;
                return ret;
            }
            if (0 <= text.IndexOf("n")) {
                var ret = double.TryParse(text.Replace("n", ""), out num);
                num *= 1e-9;
                return ret;
            }
            if (0 <= text.IndexOf("u")) {
                var ret = double.TryParse(text.Replace("u", ""), out num);
                num *= 1e-6;
                return ret;
            }
            if (0 <= text.IndexOf("m")) {
                var ret = double.TryParse(text.Replace("m", ""), out num);
                num *= 1e-3;
                return ret;
            }
            if (0 <= text.IndexOf("k")) {
                var ret = double.TryParse(text.Replace("k", ""), out num);
                num *= 1e+3;
                return ret;
            }
            if (0 <= text.IndexOf("M")) {
                var ret = double.TryParse(text.Replace("M", ""), out num);
                num *= 1e+6;
                return ret;
            }
            return double.TryParse(text, out num);
        }

        static string unitText(double v, string u, bool isShort = true) {
            double va = Math.Abs(v);
            if (va < 1e-14) {
                /* this used to return null, but then wires would display "null" with 0V */
                return "0" + u;
            }
            if (va < 1e-8) {
                return format(v * 1e12, isShort) + "p" + u;
            }
            if (va < 1e-3) {
                return format(v * 1e6, isShort) + "u" + u;
            }
            if (va < 1) {
                return format(v * 1e3, isShort) + "m" + u;
            }
            if (va < 1e3) {
                return format(v, isShort) + u;
            }
            if (va < 1e6) {
                return format(v * 1e-3, isShort) + "k" + u;
            }
            if (va < 1e9) {
                return format(v * 1e-6, isShort) + "M" + u;
            }
            return format(v * 1e-9, isShort) + "G" + u;
        }

        static string format(double v, bool isShort) {
            return isShort ? v.ToString("0.##") : v.ToString("0.00");
        }
    }
}
