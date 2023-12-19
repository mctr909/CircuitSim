using System;
using System.Collections.Generic;
using System.Linq;

namespace Circuit.Elements.Input {
	class Expr {
		enum TYPE {
			E_ADD = 1,
			E_SUB = 2,
			E_TIME = 3,
			E_VALUE = 6,
			E_MUL = 7,
			E_DIV = 8,
			E_POW = 9,
			E_UMINUS = 10,
			E_SIN = 11,
			E_COS = 12,
			E_ABS = 13,
			E_EXP = 14,
			E_LOG = 15,
			E_SQRT = 16,
			E_TAN = 17,
			E_R = 18,
			E_MAX = 19,
			E_MIN = 20,
			E_CLAMP = 21,
			E_PWL = 22,
			E_TRIANGLE = 23,
			E_SAWTOOTH = 24,
			E_MOD = 25,
			E_STEP = 26,
			E_SELECT = 27,
			E_VARIABLE = 28, /* should be at end */
		}

		TYPE type;
		double value;
		List<Expr> Children;

		public string VariableName {
			get {
				if (TYPE.E_VARIABLE <= type) {
					var ch = (char)('a' + type - TYPE.E_VARIABLE);
					return "" + ch;
				} else {
					return "Not Variable";
				}
			}
		}

		Expr(Expr e1, Expr e2, TYPE v) {
			Children = new List<Expr>() { e1 };
			if (e2 != null) {
				Children.Add(e2);
			}
			type = v;
		}

		Expr(TYPE v, double vv) {
			type = v;
			value = vv;
		}

		Expr(TYPE v) {
			type = v;
		}

		public double Eval(State es) {
			Expr left = null;
			Expr right = null;
			if (Children != null && Children.Count > 0) {
				left = Children[0];
				if (Children.Count == 2) {
					right = Children[Children.Count - 1];
				}
			}
			switch (type) {
			case TYPE.E_ADD:
				return left.Eval(es) + right.Eval(es);
			case TYPE.E_SUB:
				return left.Eval(es) - right.Eval(es);
			case TYPE.E_MUL:
				return left.Eval(es) * right.Eval(es);
			case TYPE.E_DIV:
				return left.Eval(es) / right.Eval(es);
			case TYPE.E_POW:
				return Math.Pow(left.Eval(es), right.Eval(es));
			case TYPE.E_UMINUS:
				return -left.Eval(es);
			case TYPE.E_VALUE:
				return value;
			case TYPE.E_TIME:
				return es.Time;
			case TYPE.E_SIN:
				return Math.Sin(left.Eval(es));
			case TYPE.E_COS:
				return Math.Cos(left.Eval(es));
			case TYPE.E_ABS:
				return Math.Abs(left.Eval(es));
			case TYPE.E_EXP:
				return Math.Exp(left.Eval(es));
			case TYPE.E_LOG:
				return Math.Log(left.Eval(es));
			case TYPE.E_SQRT:
				return Math.Sqrt(left.Eval(es));
			case TYPE.E_TAN:
				return Math.Tan(left.Eval(es));
			case TYPE.E_MIN: {
				int i;
				double x = left.Eval(es);
				for (i = 1; i < Children.Count; i++) {
					x = Math.Min(x, Children[i].Eval(es));
				}
				return x;
			}
			case TYPE.E_MAX: {
				int i;
				double x = left.Eval(es);
				for (i = 1; i < Children.Count; i++) {
					x = Math.Max(x, Children[i].Eval(es));
				}
				return x;
			}
			case TYPE.E_CLAMP:
				return Math.Min(Math.Max(left.Eval(es), Children[1].Eval(es)), Children[2].Eval(es));
			case TYPE.E_STEP: {
				double x = left.Eval(es);
				if (right == null) {
					return (x < 0) ? 0 : 1;
				}
				return (x > right.Eval(es)) ? 0 : (x < 0) ? 0 : 1;
			}
			case TYPE.E_SELECT: {
				double x = left.Eval(es);
				return Children[x > 0 ? 2 : 1].Eval(es);
			}
			case TYPE.E_TRIANGLE: {
				double x = posmod(left.Eval(es), Math.PI * 2) / Math.PI;
				return (x < 1) ? -1 + x * 2 : 3 - x * 2;
			}
			case TYPE.E_SAWTOOTH: {
				double x = posmod(left.Eval(es), Math.PI * 2) / Math.PI;
				return x - 1;
			}
			case TYPE.E_MOD:
				return left.Eval(es) % right.Eval(es);
			case TYPE.E_PWL:
				return pwl(es, Children);
			default:
				if (type >= TYPE.E_VARIABLE) {
					return es.Values[type - TYPE.E_VARIABLE];
				}
				Console.WriteLine("unknown\n");
				break;
			}
			return 0;
		}

		double pwl(State es, List<Expr> args) {
			double x = args[0].Eval(es);
			double x0 = args[1].Eval(es);
			double y0 = args[2].Eval(es);
			if (x < x0) {
				return y0;
			}
			double x1 = args[3].Eval(es);
			double y1 = args[4].Eval(es);
			int i = 5;
			while (true) {
				if (x < x1) {
					return y0 + (x - x0) * (y1 - y0) / (x1 - x0);
				}
				if (i + 1 >= args.Count) {
					break;
				}
				x0 = x1;
				y0 = y1;
				x1 = args[i].Eval(es);
				y1 = args[i + 1].Eval(es);
				i += 2;
			}
			return y1;
		}

		double posmod(double x, double y) {
			x %= y;
			return (x >= 0) ? x : x + y;
		}

		public class State {
			public int N;
			public double[] Values;
			public double Time;
			public State(int xx) {
				N = xx;
				Values = new double[9];
				Values[4] = Math.E;
			}
		}

		public class Parser {
			string text;
			string token;
			int pos;
			int tlen;
			bool err;

			public Parser(string s) {
				text = s.ToLower().Replace(" ", "").Replace("{", "(").Replace("}", ")").Replace("\r", "").Replace("\n", "");
				tlen = text.Length;
				pos = 0;
				err = false;
				getToken();
			}

			public Expr ParseExpression() {
				if (token.Length == 0) {
					return new Expr(TYPE.E_VALUE, 0.0);
				}
				var e = parse();
				if (token.Length > 0) {
					err = true;
				}
				return e;
			}

			bool gotError() { return err; }

			void getToken() {
				while (pos < tlen && text.ElementAt(pos) == ' ') {
					pos++;
				}
				if (pos == tlen) {
					token = "";
					return;
				}
				int i = pos;
				int c = text.ElementAt(i);
				if ((c >= '0' && c <= '9') || c == '.') {
					for (i = pos; i != tlen; i++) {
						if (text.ElementAt(i) == 'e' || text.ElementAt(i) == 'E') {
							i++;
							if (i < tlen && (text.ElementAt(i) == '+' || text.ElementAt(i) == '-')) {
								i++;
							}
						}
						if (!((text.ElementAt(i) >= '0' && text.ElementAt(i) <= '9') || text.ElementAt(i) == '.')) {
							break;
						}
					}
				} else if (c >= 'a' && c <= 'z') {
					for (i = pos; i != tlen; i++) {
						if (!(text.ElementAt(i) >= 'a' && text.ElementAt(i) <= 'z')) {
							break;
						}
					}
				} else {
					i++;
				}
				token = text.Substring(pos, i - pos);
				pos = i;
			}

			bool skip(string s) {
				if (token.CompareTo(s) != 0) {
					return false;
				}
				getToken();
				return true;
			}

			void skipOrError(string s) {
				if (!skip(s)) {
					err = true;
				}
			}

			Expr parse() {
				var e = parseMult();
				while (true) {
					if (skip("+")) {
						e = new Expr(e, parseMult(), TYPE.E_ADD);
					} else if (skip("-")) {
						e = new Expr(e, parseMult(), TYPE.E_SUB);
					} else {
						break;
					}
				}
				return e;
			}

			Expr parseMult() {
				var e = parseUminus();
				while (true) {
					if (skip("*")) {
						e = new Expr(e, parseUminus(), TYPE.E_MUL);
					} else if (skip("/")) {
						e = new Expr(e, parseUminus(), TYPE.E_DIV);
					} else {
						break;
					}
				}
				return e;
			}

			Expr parseUminus() {
				skip("+");
				if (skip("-")) {
					return new Expr(parsePow(), null, TYPE.E_UMINUS);
				}
				return parsePow();
			}

			Expr parsePow() {
				var e = parseTerm();
				while (true) {
					if (skip("^")) {
						e = new Expr(e, parseTerm(), TYPE.E_POW);
					} else {
						break;
					}
				}
				return e;
			}

			Expr parseFunc(TYPE t) {
				skipOrError("(");
				var e = parse();
				skipOrError(")");
				return new Expr(e, null, t);
			}

			Expr parseFuncMulti(TYPE t, int minArgs, int maxArgs) {
				int args = 1;
				skipOrError("(");
				var e1 = parse();
				var e = new Expr(e1, null, t);
				while (skip(",")) {
					var enext = parse();
					e.Children.Add(enext);
					args++;
				}
				skipOrError(")");
				if (args < minArgs || args > maxArgs) {
					err = true;
				}
				return e;
			}

			Expr parseTerm() {
				if (skip("(")) {
					var e = parse();
					skipOrError(")");
					return e;
				}
				if (skip("t")) {
					return new Expr(TYPE.E_TIME);
				}
				if (token.Length == 1) {
					char c = token.ElementAt(0);
					if (c >= 'a' && c <= 'i') {
						getToken();
						return new Expr(TYPE.E_VARIABLE + (c - 'a'));
					}
				}
				/*if (skip("e"))
                    return new Expr(EXPR_TYPE.E_VALUE, 2.7182818284590452354);*/
				if (skip("pi"))
					return new Expr(TYPE.E_VALUE, 3.14159265358979323846);
				if (skip("sin"))
					return parseFunc(TYPE.E_SIN);
				if (skip("cos"))
					return parseFunc(TYPE.E_COS);
				if (skip("abs"))
					return parseFunc(TYPE.E_ABS);
				if (skip("exp"))
					return parseFunc(TYPE.E_EXP);
				if (skip("log"))
					return parseFunc(TYPE.E_LOG);
				if (skip("sqrt"))
					return parseFunc(TYPE.E_SQRT);
				if (skip("tan"))
					return parseFunc(TYPE.E_TAN);
				if (skip("tri"))
					return parseFunc(TYPE.E_TRIANGLE);
				if (skip("saw"))
					return parseFunc(TYPE.E_SAWTOOTH);
				if (skip("min"))
					return parseFuncMulti(TYPE.E_MIN, 2, 1000);
				if (skip("max"))
					return parseFuncMulti(TYPE.E_MAX, 2, 1000);
				if (skip("pwl"))
					return parseFuncMulti(TYPE.E_PWL, 2, 1000);
				if (skip("mod"))
					return parseFuncMulti(TYPE.E_MOD, 2, 2);
				if (skip("step"))
					return parseFuncMulti(TYPE.E_STEP, 1, 2);
				if (skip("select"))
					return parseFuncMulti(TYPE.E_SELECT, 3, 3);
				if (skip("clamp"))
					return parseFuncMulti(TYPE.E_CLAMP, 3, 3);
				try {
					var e = new Expr(TYPE.E_VALUE, double.Parse(token));
					getToken();
					return e;
				} catch (Exception e) {
					err = true;
					Console.WriteLine("unrecognized token: " + token);
					Console.WriteLine(e.Message);
					Console.WriteLine(e.StackTrace);
					return new Expr(TYPE.E_VALUE, 0);
				}
			}
		}
	}
}
