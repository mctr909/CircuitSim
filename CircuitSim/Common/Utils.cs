using System;
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

        /* factors a matrix into upper and lower triangular matrices by
        /* gaussian elimination.  On entry, a[0..n-1][0..n-1] is the
        /* matrix to be factored.  ipvt[] returns an integer vector of pivot
        /* indices, used in the lu_solve() routine. */
        public static bool luFactor(double[,] a, int n, int[] ipvt) {
            /* check for a possible singular matrix by scanning for rows that
            /* are all zeroes */
            for (int i = 0; i != n; i++) {
                bool row_all_zeros = true;
                for (int j = 0; j != n; j++) {
                    if (a[i, j] != 0) {
                        row_all_zeros = false;
                        break;
                    }
                }
                /* if all zeros, it's a singular matrix */
                if (row_all_zeros) {
                    return false;
                }
            }

            /* use Crout's method; loop through the columns */
            for (int j = 0; j != n; j++) {
                /* calculate upper triangular elements for this column */
                for (int i = 0; i != j; i++) {
                    double q = a[i, j];
                    for (int k = 0; k != i; k++) {
                        q -= a[i, k] * a[k, j];
                    }
                    a[i, j] = q;
                }
                /* calculate lower triangular elements for this column */
                double largest = 0;
                int largestRow = -1;
                for (int i = j; i != n; i++) {
                    double q = a[i, j];
                    for (int k = 0; k != j; k++) {
                        q -= a[i, k] * a[k, j];
                    }
                    a[i, j] = q;
                    double x = Math.Abs(q);
                    if (x >= largest) {
                        largest = x;
                        largestRow = i;
                    }
                }
                /* pivoting */
                if (j != largestRow) {
                    double x;
                    for (int k = 0; k != n; k++) {
                        x = a[largestRow, k];
                        a[largestRow, k] = a[j, k];
                        a[j, k] = x;
                    }
                }
                /* keep track of row interchanges */
                ipvt[j] = largestRow;
                /* avoid zeros */
                if (a[j, j] == 0.0) {
                    Console.WriteLine("avoided zero");
                    a[j, j] = 1e-18;
                }
                if (j != n - 1) {
                    double mult = 1.0 / a[j, j];
                    for (int i = j + 1; i != n; i++) {
                        a[i, j] *= mult;
                    }
                }
            }
            return true;
        }

        /* Solves the set of n linear equations using a LU factorization
        /* previously performed by lu_factor.  On input, b[0..n-1] is the right
        /* hand side of the equations, and on output, contains the solution. */
        public static void luSolve(double[,] a, int n, int[] ipvt, double[] b) {
            int i;

            /* find first nonzero b element */
            for (i = 0; i != n; i++) {
                int row = ipvt[i];
                double swap = b[row];
                b[row] = b[i];
                b[i] = swap;
                if (swap != 0) {
                    break;
                }
            }

            int bi = i++;
            for (; i < n; i++) {
                int row = ipvt[i];
                double tot = b[row];
                b[row] = b[i];
                /* forward substitution using the lower triangular matrix */
                for (int j = bi; j < i; j++) {
                    tot -= a[i, j] * b[j];
                }
                b[i] = tot;
            }
            for (i = n - 1; i >= 0; i--) {
                double tot = b[i];
                /* back-substitution using the upper triangular matrix */
                for (int j = i + 1; j != n; j++) {
                    tot -= a[i, j] * b[j];
                }
                b[i] = tot / a[i, i];
            }
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
            return unitText(Math.Abs(v), "V", false);
        }

        public static string CurrentText(double i) {
            return unitText(i, "A", false);
        }

        public static string CurrentAbsText(double i) {
            return unitText(Math.Abs(i), "A", false);
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
            return unitText(v, "s", false);
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
