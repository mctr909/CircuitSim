using System;
using System.Collections.Generic;
using System.Linq;

namespace Circuit {
    class ExprState {
        public int n;
        public double[] values;
        public double t;
        public ExprState(int xx) {
            n = xx;
            values = new double[9];
            values[4] = Math.E;
        }
    }

    class Expr {
        public const int E_ADD = 1;
        public const int E_SUB = 2;
        public const int E_T = 3;
        public const int E_VAL = 6;
        public const int E_MUL = 7;
        public const int E_DIV = 8;
        public const int E_POW = 9;
        public const int E_UMINUS = 10;
        public const int E_SIN = 11;
        public const int E_COS = 12;
        public const int E_ABS = 13;
        public const int E_EXP = 14;
        public const int E_LOG = 15;
        public const int E_SQRT = 16;
        public const int E_TAN = 17;
        public const int E_R = 18;
        public const int E_MAX = 19;
        public const int E_MIN = 20;
        public const int E_CLAMP = 21;
        public const int E_PWL = 22;
        public const int E_TRIANGLE = 23;
        public const int E_SAWTOOTH = 24;
        public const int E_MOD = 25;
        public const int E_STEP = 26;
        public const int E_SELECT = 27;
        public const int E_A = 28; /* should be at end */

        public List<Expr> children;
        double value;
        int type;

        public Expr(Expr e1, Expr e2, int v) {
            children = new List<Expr>();
            children.Add(e1);
            if (e2 != null) {
                children.Add(e2);
            }
            type = v;
        }

        public Expr(int v, double vv) {
            type = v;
            value = vv;
        }

        public Expr(int v) {
            type = v;
        }

        public double eval(ExprState es) {
            Expr left = null;
            Expr right = null;
            if (children != null && children.Count > 0) {
                left = children[0];
                if (children.Count == 2) {
                    right = children[children.Count - 1];
                }
            }
            switch (type) {
            case E_ADD:
                return left.eval(es) + right.eval(es);
            case E_SUB:
                return left.eval(es) - right.eval(es);
            case E_MUL:
                return left.eval(es) * right.eval(es);
            case E_DIV:
                return left.eval(es) / right.eval(es);
            case E_POW:
                return Math.Pow(left.eval(es), right.eval(es));
            case E_UMINUS:
                return -left.eval(es);
            case E_VAL:
                return value;
            case E_T:
                return es.t;
            case E_SIN:
                return Math.Sin(left.eval(es));
            case E_COS:
                return Math.Cos(left.eval(es));
            case E_ABS:
                return Math.Abs(left.eval(es));
            case E_EXP:
                return Math.Exp(left.eval(es));
            case E_LOG:
                return Math.Log(left.eval(es));
            case E_SQRT:
                return Math.Sqrt(left.eval(es));
            case E_TAN:
                return Math.Tan(left.eval(es));
            case E_MIN: {
                int i;
                double x = left.eval(es);
                for (i = 1; i < children.Count; i++) {
                    x = Math.Min(x, children[i].eval(es));
                }
                return x;
            }
            case E_MAX: {
                int i;
                double x = left.eval(es);
                for (i = 1; i < children.Count; i++) {
                    x = Math.Max(x, children[i].eval(es));
                }
                return x;
            }
            case E_CLAMP:
                return Math.Min(Math.Max(left.eval(es), children[1].eval(es)), children[2].eval(es));
            case E_STEP: {
                double x = left.eval(es);
                if (right == null) {
                    return (x < 0) ? 0 : 1;
                }
                return (x > right.eval(es)) ? 0 : (x < 0) ? 0 : 1;
            }
            case E_SELECT: {
                double x = left.eval(es);
                return children[x > 0 ? 2 : 1].eval(es);
            }
            case E_TRIANGLE: {
                double x = posmod(left.eval(es), Math.PI * 2) / Math.PI;
                return (x < 1) ? -1 + x * 2 : 3 - x * 2;
            }
            case E_SAWTOOTH: {
                double x = posmod(left.eval(es), Math.PI * 2) / Math.PI;
                return x - 1;
            }
            case E_MOD:
                return left.eval(es) % right.eval(es);
            case E_PWL:
                return pwl(es, children);
            default:
                if (type >= E_A) {
                    return es.values[type - E_A];
                }
                Console.WriteLine("unknown\n");
                break;
            }
            return 0;
        }

        double pwl(ExprState es, List<Expr> args) {
            double x = args[0].eval(es);
            double x0 = args[1].eval(es);
            double y0 = args[2].eval(es);
            if (x < x0) {
                return y0;
            }
            double x1 = args[3].eval(es);
            double y1 = args[4].eval(es);
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
                x1 = args[i].eval(es);
                y1 = args[i + 1].eval(es);
                i += 2;
            }
            return y1;
        }

        double posmod(double x, double y) {
            x %= y;
            return (x >= 0) ? x : x + y;
        }
    }

    class ExprParser {
        string text;
        string token;
        int pos;
        int tlen;
        bool err;

        public ExprParser(string s) {
            text = s.ToLower();
            tlen = text.Length;
            pos = 0;
            err = false;
            getToken();
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
            token = text.Substring(pos, i);
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

        public Expr parseExpression() {
            if (token.Length == 0) {
                return new Expr(Expr.E_VAL, 0.0);
            }
            var e = parse();
            if (token.Length > 0) {
                err = true;
            }
            return e;
        }

        Expr parse() {
            var e = parseMult();
            while (true) {
                if (skip("+")) {
                    e = new Expr(e, parseMult(), Expr.E_ADD);
                } else if (skip("-")) {
                    e = new Expr(e, parseMult(), Expr.E_SUB);
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
                    e = new Expr(e, parseUminus(), Expr.E_MUL);
                } else if (skip("/")) {
                    e = new Expr(e, parseUminus(), Expr.E_DIV);
                } else {
                    break;
                }
            }
            return e;
        }

        Expr parseUminus() {
            skip("+");
            if (skip("-")) {
                return new Expr(parsePow(), null, Expr.E_UMINUS);
            }
            return parsePow();
        }

        Expr parsePow() {
            var e = parseTerm();
            while (true) {
                if (skip("^")) {
                    e = new Expr(e, parseTerm(), Expr.E_POW);
                } else {
                    break;
                }
            }
            return e;
        }

        Expr parseFunc(int t) {
            skipOrError("(");
            var e = parse();
            skipOrError(")");
            return new Expr(e, null, t);
        }

        Expr parseFuncMulti(int t, int minArgs, int maxArgs) {
            int args = 1;
            skipOrError("(");
            var e1 = parse();
            var e = new Expr(e1, null, t);
            while (skip(",")) {
                var enext = parse();
                e.children.Add(enext);
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
                return new Expr(Expr.E_T);
            }
            if (token.Length == 1) {
                char c = token.ElementAt(0);
                if (c >= 'a' && c <= 'i') {
                    getToken();
                    return new Expr(Expr.E_A + (c - 'a'));
                }
            }
            if (skip("pi"))
                return new Expr(Expr.E_VAL, 3.14159265358979323846);
            /*if (skip("e"))
                return new Expr(Expr.E_VAL, 2.7182818284590452354);*/
            if (skip("sin"))
                return parseFunc(Expr.E_SIN);
            if (skip("cos"))
                return parseFunc(Expr.E_COS);
            if (skip("abs"))
                return parseFunc(Expr.E_ABS);
            if (skip("exp"))
                return parseFunc(Expr.E_EXP);
            if (skip("log"))
                return parseFunc(Expr.E_LOG);
            if (skip("sqrt"))
                return parseFunc(Expr.E_SQRT);
            if (skip("tan"))
                return parseFunc(Expr.E_TAN);
            if (skip("tri"))
                return parseFunc(Expr.E_TRIANGLE);
            if (skip("saw"))
                return parseFunc(Expr.E_SAWTOOTH);
            if (skip("min"))
                return parseFuncMulti(Expr.E_MIN, 2, 1000);
            if (skip("max"))
                return parseFuncMulti(Expr.E_MAX, 2, 1000);
            if (skip("pwl"))
                return parseFuncMulti(Expr.E_PWL, 2, 1000);
            if (skip("mod"))
                return parseFuncMulti(Expr.E_MOD, 2, 2);
            if (skip("step"))
                return parseFuncMulti(Expr.E_STEP, 1, 2);
            if (skip("select"))
                return parseFuncMulti(Expr.E_SELECT, 3, 3);
            if (skip("clamp"))
                return parseFuncMulti(Expr.E_CLAMP, 3, 3);
            try {
                var e = new Expr(Expr.E_VAL, double.Parse(token));
                getToken();
                return e;
            } catch (Exception e) {
                err = true;
                Console.WriteLine("unrecognized token: " + token + "\n");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return new Expr(Expr.E_VAL, 0);
            }
        }
    }
}
