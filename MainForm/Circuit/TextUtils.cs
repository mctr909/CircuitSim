using System.Text.RegularExpressions;

namespace Circuit {
	enum EScale {
		AUTO,
		FIXED,
		FIXED_MILLI,
		FIXED_MICRO,
	}

	static class TextUtils {
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

		public static string UnEscape(string s) {
			if (s == "\\0") {
				return "";
			}
			for (int i = 0; i < s.Length; i++) {
				if (s.ElementAt(i) == '\\') {
					var c = s.ElementAt(i + 1);
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

		public static string Voltage(double v) {
			return FormatWithUnit(v, "V", false);
		}

		public static string VoltageAbs(double v) {
			return FormatWithUnit(Math.Abs(v), "V", false, false);
		}

		public static string Current(double i) {
			return FormatWithUnit(i, "A", false);
		}

		public static string CurrentAbs(double i) {
			return FormatWithUnit(Math.Abs(i), "A", false, false);
		}

		public static string Frequency(double v, bool isShort = false) {
			return FormatWithUnit(v, "Hz", isShort, false);
		}

		public static string Phase(double rad) {
			if (rad < -Math.PI) {
				rad += Math.PI * 2;
			}
			if (Math.PI < rad) {
				rad -= Math.PI * 2;
			}
			return FormatWithUnit(rad * 180 / Math.PI, "deg");
		}

		public static string Time(double v) {
			if (v >= 60) {
				var h = Math.Floor(v / 3600);
				v -= 3600 * h;
				var m = Math.Floor(v / 60);
				v -= 60 * m;
				if (h == 0) {
					return m + ":" + ((v >= 10) ? "" : "0") + v.ToString("0.00");
				}
				return h + ":" + ((m >= 10) ? "" : "0") + m + ":" + ((v >= 10) ? "" : "0") + v.ToString("0.00");
			}
			return FormatWithUnit(v, "s", false, false);
		}

		public static string Unit(double v, string u = "") {
			return FormatWithUnit(v, u);
		}

		public static string Unit3digit(double v, string u = "", bool sign = true) {
			return FormatWithUnit(v, u, false, sign);
		}

		public static string UnitWithScale(double val, string utext, EScale scale) {
			if (scale == EScale.FIXED) {
				return val.ToString("+0.00;-0.00; 0.00") + utext;
			}
			if (scale == EScale.FIXED_MILLI) {
				return (1e3 * val).ToString("+0.00;-0.00; 0.00") + "m" + utext;
			}
			if (scale == EScale.FIXED_MICRO) {
				return (1e6 * val).ToString("+0.00;-0.00; 0.00") + "u" + utext;
			}
			return FormatWithUnit(val, utext, false);
		}

		public static bool ParseUnits(string s, out double ret) {
			s = s.Trim();
			if ("" == s) {
				ret = 0;
				return false;
			}
			var rg = new Regex("([0-9]+)([pnumkMG])([0-9]+)");
			s = rg.Replace(s, "$1.$3$2");
			var len = s.Length;
			var chrU = s.ElementAt(len - 1);
			var unit = 1.0;
			switch (chrU) {
			case 'p':
				unit = 1e-12;
				break;
			case 'n':
				unit = 1e-9;
				break;
			case 'u':
				unit = 1e-6;
				break;
			case 'm':
				unit = 1e-3;
				break;
			case 'k':
				unit = 1e3;
				break;
			case 'M':
				unit = 1e6;
				break;
			case 'G':
				unit = 1e9;
				break;
			}
			if (unit != 1) {
				s = s.Substring(0, len - 1).Trim();
			}
			if (double.TryParse(s, out ret)) {
				ret *= unit;
				return true;
			} else {
				ret = 0;
				return false;
			}
		}

		static string FormatWithUnit(double v, string u, bool isShort = true, bool sign = true) {
			var va = Math.Abs(v);
			if (va < 1e-14) {
				return (isShort ? "0" : (sign ? " 0.00" : "0.00")) + u;
			}
			if (va < 1e-9) {
				return Format(v * 1e12, isShort, sign) + "p" + u;
			}
			if (va < 1e-6) {
				return Format(v * 1e9, isShort, sign) + "n" + u;
			}
			if (va < 1e-3) {
				return Format(v * 1e6, isShort, sign) + "u" + u;
			}
			if (va < 1) {
				return Format(v * 1e3, isShort, sign) + "m" + u;
			}
			if (va < 1e3) {
				return Format(v, isShort, sign) + u;
			}
			if (va < 1e6) {
				return Format(v * 1e-3, isShort, sign) + "k" + u;
			}
			if (va < 1e9) {
				return Format(v * 1e-6, isShort, sign) + "M" + u;
			}
			return Format(v * 1e-9, isShort, sign) + "G" + u;
		}

		static string Format(double v, bool isShort, bool sign) {
			var s = double.Parse(v.ToString("g3"));
			return isShort ? s.ToString("0.##") : s.ToString(sign ? "+0.##;-0.##" : "0.##");
		}
	}
}
