﻿using System;
using System.Drawing;
using System.Linq;

namespace Circuit {
    enum E_SCALE {
        AUTO,
        X1,
        M,
        MU,
    }

    static class Utils {
        public static double Angle(PointF o, PointF p) {
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

        public static double Distance(PointF a, PointF b) {
            var x = b.X - a.X;
            var y = b.Y - a.Y;
            return Math.Sqrt(x * x + y * y);
        }

        public static double Distance(double ax, double ay, PointF b) {
            var x = b.X - ax;
            var y = b.Y - ay;
            return Math.Sqrt(x * x + y * y);
        }

        public static double Distance(PointF a, double bx, double by) {
            var x = bx - a.X;
            var y = by - a.Y;
            return Math.Sqrt(x * x + y * y);
        }

        public static double Distance(double ax, double ay, double bx, double by) {
            var x = bx - ax;
            var y = by - ay;
            return Math.Sqrt(x * x + y * y);
        }

        public static double DistanceOnLine(PointF a, PointF b, double px, double py) {
            return DistanceOnLine(a.X, a.Y, b.X, b.Y, px, py);
        }

        public static double DistanceOnLine(PointF a, PointF b, PointF p) {
            return DistanceOnLine(a.X, a.Y, b.X, b.Y, p.X, p.Y);
        }

        public static double DistanceOnLine(double ax, double ay, double bx, double by, double px, double py) {
            var abx = bx - ax;
            var aby = by - ay;
            if (0 == abx * abx + aby * aby) {
                px -= ax;
                py -= ay;
                return Math.Sqrt(px * px + py * py);
            }
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
        public static void InterpPoint(PointF a, PointF b, ref PointF ret, double f) {
            ret.X = (float)(a.X * (1 - f) + b.X * f);
            ret.Y = (float)(a.Y * (1 - f) + b.Y * f);
        }

        /// <summary>
        /// Returns a point fraction f along the line between a and b and offset perpendicular by g
        /// </summary>
        /// <param name="a">1st Point</param>
        /// <param name="b">2nd Point</param>
        /// <param name="ret">Returns interpolated point</param>
        /// <param name="f">Fraction along line</param>
        /// <param name="g">Fraction perpendicular to line</param>
        public static void InterpPoint(PointF a, PointF b, ref PointF ret, double f, double g) {
            var gx = b.Y - a.Y;
            var gy = a.X - b.X;
            var r = Math.Sqrt(gx * gx + gy * gy);
            if (0.0 == r) {
                ret.X = a.X;
                ret.Y = a.Y;
            } else {
                g /= r;
                ret.X = (float)(a.X * (1 - f) + b.X * f + g * gx);
                ret.Y = (float)(a.Y * (1 - f) + b.Y * f + g * gy);
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
        public static void InterpPoint(PointF a, PointF b, ref PointF ret1, ref PointF ret2, double f, double g) {
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
                ret1.X = (float)(a.X * (1 - f) + b.X * f + g * gx);
                ret1.Y = (float)(a.Y * (1 - f) + b.Y * f + g * gy);
                ret2.X = (float)(a.X * (1 - f) + b.X * f - g * gx);
                ret2.Y = (float)(a.Y * (1 - f) + b.Y * f - g * gy);
            }
        }

        public static void CreateArrow(PointF a, PointF b, out PointF[] ret, double al, double aw) {
            var adx = b.X - a.X;
            var ady = b.Y - a.Y;
            var l = Math.Sqrt(adx * adx + ady * ady);
            ret = new PointF[3];
            ret[0] = new PointF(b.X, b.Y);
            InterpPoint(a, b, ref ret[1], ref ret[2], 1.0 - al / l, aw);
        }

        public static void CreateSchmitt(PointF a, PointF b, out PointF[] ret, double gsize, double ctr) {
            ret = new PointF[6];
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

        public static string Escape(string s) {
            if (s.Length == 0) {
                return "\\0";
            }
            return s.Replace("\\", "\\\\")
                .Replace("\n", "\\n")
                .Replace("+", "\\p")
                .Replace("=", "\\q")
                .Replace("#", "\\h")
                .Replace("&", "\\a")
                .Replace("\r", "\\r")
                .Replace(" ", "\\s");
        }

        public static string Unescape(string s) {
            if (s == "\\0") {
                return "";
            }
            for (int i = 0; i < s.Length; i++) {
                if (s.ElementAt(i) == '\\') {
                    char c = s.ElementAt(i + 1);
                    if (c == 'n') {
                        s = s.Substring(0, i) + "\n" + s.Substring(i + 2);
                    } else if (c == 'r') {
                        s = s.Substring(0, i) + "\r" + s.Substring(i + 2);
                    } else if (c == 's') {
                        s = s.Substring(0, i) + " " + s.Substring(i + 2);
                    } else if (c == 'p') {
                        s = s.Substring(0, i) + "+" + s.Substring(i + 2);
                    } else if (c == 'q') {
                        s = s.Substring(0, i) + "=" + s.Substring(i + 2);
                    } else if (c == 'h') {
                        s = s.Substring(0, i) + "#" + s.Substring(i + 2);
                    } else if (c == 'a') {
                        s = s.Substring(0, i) + "&" + s.Substring(i + 2);
                    } else {
                        s = s.Substring(0, i) + s.Substring(i + 1);
                    }
                }
            }
            return s;
        }

        public static string VoltageText(double v) {
            return unitText(v, "V", false);
        }

        public static string VoltageAbsText(double v) {
            return unitText(Math.Abs(v), "V", false, false);
        }

        public static string CurrentText(double i) {
            return unitText(i, "A", false);
        }

        public static string CurrentAbsText(double i) {
            return unitText(Math.Abs(i), "A", false, false);
        }

        public static string FrequencyText(double v, bool isShort = false) {
            return unitText(v, "Hz", isShort, false);
        }

        public static string PhaseText(double rad) {
            if (rad < -Math.PI) {
                rad += Math.PI * 2;
            }
            if (Math.PI < rad) {
                rad -= Math.PI * 2;
            }
            return unitText(rad * 180 / Math.PI, "deg");
        }

        public static string UnitText(double v, string u = "") {
            return unitText(v, u);
        }

        public static string UnitText3digit(double v, string u = "", bool sign = true) {
            return unitText(v, u, false, sign);
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
            return unitText(v, "s", false, false);
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

        static string unitText(double v, string u, bool isShort = true, bool sign = true) {
            double va = Math.Abs(v);
            if (va < 1e-14) {
                return (isShort ? "0" : (sign ? " 0.00" : "0.00")) + u;
            }
            if (va < 1e-8) {
                return format(v * 1e12, isShort, sign) + "p" + u;
            }
            if (va < 1e-3) {
                return format(v * 1e6, isShort, sign) + "u" + u;
            }
            if (va < 1) {
                return format(v * 1e3, isShort, sign) + "m" + u;
            }
            if (va < 1e3) {
                return format(v, isShort, sign) + u;
            }
            if (va < 1e6) {
                return format(v * 1e-3, isShort, sign) + "k" + u;
            }
            if (va < 1e9) {
                return format(v * 1e-6, isShort, sign) + "M" + u;
            }
            return format(v * 1e-9, isShort, sign) + "G" + u;
        }

        static string format(double v, bool isShort, bool sign) {
            return isShort ? v.ToString("0.##") : v.ToString(sign ? "+0.00;-0.00" : "0.00");
        }
    }
}
