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

    enum EXPR_TYPE {
        E_ADD = 1,
        E_SUB = 2,
        E_T = 3,
        E_VAL = 6,
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
        E_A = 28, /* should be at end */
    }

    class Expr {
        public List<Expr> children;
        double value;
        EXPR_TYPE type;

        public Expr(Expr e1, Expr e2, EXPR_TYPE v) {
            children = new List<Expr>();
            children.Add(e1);
            if (e2 != null) {
                children.Add(e2);
            }
            type = v;
        }

        public Expr(EXPR_TYPE v, double vv) {
            type = v;
            value = vv;
        }

        public Expr(EXPR_TYPE v) {
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
            case EXPR_TYPE.E_ADD:
                return left.eval(es) + right.eval(es);
            case EXPR_TYPE.E_SUB:
                return left.eval(es) - right.eval(es);
            case EXPR_TYPE.E_MUL:
                return left.eval(es) * right.eval(es);
            case EXPR_TYPE.E_DIV:
                return left.eval(es) / right.eval(es);
            case EXPR_TYPE.E_POW:
                return Math.Pow(left.eval(es), right.eval(es));
            case EXPR_TYPE.E_UMINUS:
                return -left.eval(es);
            case EXPR_TYPE.E_VAL:
                return value;
            case EXPR_TYPE.E_T:
                return es.t;
            case EXPR_TYPE.E_SIN:
                return Math.Sin(left.eval(es));
            case EXPR_TYPE.E_COS:
                return Math.Cos(left.eval(es));
            case EXPR_TYPE.E_ABS:
                return Math.Abs(left.eval(es));
            case EXPR_TYPE.E_EXP:
                return Math.Exp(left.eval(es));
            case EXPR_TYPE.E_LOG:
                return Math.Log(left.eval(es));
            case EXPR_TYPE.E_SQRT:
                return Math.Sqrt(left.eval(es));
            case EXPR_TYPE.E_TAN:
                return Math.Tan(left.eval(es));
            case EXPR_TYPE.E_MIN: {
                int i;
                double x = left.eval(es);
                for (i = 1; i < children.Count; i++) {
                    x = Math.Min(x, children[i].eval(es));
                }
                return x;
            }
            case EXPR_TYPE.E_MAX: {
                int i;
                double x = left.eval(es);
                for (i = 1; i < children.Count; i++) {
                    x = Math.Max(x, children[i].eval(es));
                }
                return x;
            }
            case EXPR_TYPE.E_CLAMP:
                return Math.Min(Math.Max(left.eval(es), children[1].eval(es)), children[2].eval(es));
            case EXPR_TYPE.E_STEP: {
                double x = left.eval(es);
                if (right == null) {
                    return (x < 0) ? 0 : 1;
                }
                return (x > right.eval(es)) ? 0 : (x < 0) ? 0 : 1;
            }
            case EXPR_TYPE.E_SELECT: {
                double x = left.eval(es);
                return children[x > 0 ? 2 : 1].eval(es);
            }
            case EXPR_TYPE.E_TRIANGLE: {
                double x = posmod(left.eval(es), Math.PI * 2) / Math.PI;
                return (x < 1) ? -1 + x * 2 : 3 - x * 2;
            }
            case EXPR_TYPE.E_SAWTOOTH: {
                double x = posmod(left.eval(es), Math.PI * 2) / Math.PI;
                return x - 1;
            }
            case EXPR_TYPE.E_MOD:
                return left.eval(es) % right.eval(es);
            case EXPR_TYPE.E_PWL:
                return pwl(es, children);
            default:
                if (type >= EXPR_TYPE.E_A) {
                    return es.values[type - EXPR_TYPE.E_A];
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
            text = s.ToLower().Replace(" ", "").Replace("{", "(").Replace("}", ")").Replace("\r", "").Replace("\n", "");
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

        public Expr parseExpression() {
            if (token.Length == 0) {
                return new Expr(EXPR_TYPE.E_VAL, 0.0);
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
                    e = new Expr(e, parseMult(), EXPR_TYPE.E_ADD);
                } else if (skip("-")) {
                    e = new Expr(e, parseMult(), EXPR_TYPE.E_SUB);
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
                    e = new Expr(e, parseUminus(), EXPR_TYPE.E_MUL);
                } else if (skip("/")) {
                    e = new Expr(e, parseUminus(), EXPR_TYPE.E_DIV);
                } else {
                    break;
                }
            }
            return e;
        }

        Expr parseUminus() {
            skip("+");
            if (skip("-")) {
                return new Expr(parsePow(), null, EXPR_TYPE.E_UMINUS);
            }
            return parsePow();
        }

        Expr parsePow() {
            var e = parseTerm();
            while (true) {
                if (skip("^")) {
                    e = new Expr(e, parseTerm(), EXPR_TYPE.E_POW);
                } else {
                    break;
                }
            }
            return e;
        }

        Expr parseFunc(EXPR_TYPE t) {
            skipOrError("(");
            var e = parse();
            skipOrError(")");
            return new Expr(e, null, t);
        }

        Expr parseFuncMulti(EXPR_TYPE t, int minArgs, int maxArgs) {
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
                return new Expr(EXPR_TYPE.E_T);
            }
            if (token.Length == 1) {
                char c = token.ElementAt(0);
                if (c >= 'a' && c <= 'i') {
                    getToken();
                    return new Expr(EXPR_TYPE.E_A + (c - 'a'));
                }
            }
            if (skip("pi"))
                return new Expr(EXPR_TYPE.E_VAL, 3.14159265358979323846);
            /*if (skip("e"))
                return new Expr(EXPR_TYPE.E_VAL, 2.7182818284590452354);*/
            if (skip("sin"))
                return parseFunc(EXPR_TYPE.E_SIN);
            if (skip("cos"))
                return parseFunc(EXPR_TYPE.E_COS);
            if (skip("abs"))
                return parseFunc(EXPR_TYPE.E_ABS);
            if (skip("exp"))
                return parseFunc(EXPR_TYPE.E_EXP);
            if (skip("log"))
                return parseFunc(EXPR_TYPE.E_LOG);
            if (skip("sqrt"))
                return parseFunc(EXPR_TYPE.E_SQRT);
            if (skip("tan"))
                return parseFunc(EXPR_TYPE.E_TAN);
            if (skip("tri"))
                return parseFunc(EXPR_TYPE.E_TRIANGLE);
            if (skip("saw"))
                return parseFunc(EXPR_TYPE.E_SAWTOOTH);
            if (skip("min"))
                return parseFuncMulti(EXPR_TYPE.E_MIN, 2, 1000);
            if (skip("max"))
                return parseFuncMulti(EXPR_TYPE.E_MAX, 2, 1000);
            if (skip("pwl"))
                return parseFuncMulti(EXPR_TYPE.E_PWL, 2, 1000);
            if (skip("mod"))
                return parseFuncMulti(EXPR_TYPE.E_MOD, 2, 2);
            if (skip("step"))
                return parseFuncMulti(EXPR_TYPE.E_STEP, 1, 2);
            if (skip("select"))
                return parseFuncMulti(EXPR_TYPE.E_SELECT, 3, 3);
            if (skip("clamp"))
                return parseFuncMulti(EXPR_TYPE.E_CLAMP, 3, 3);
            //try {
            {
                var e = new Expr(EXPR_TYPE.E_VAL, double.Parse(token));
                getToken();
                return e;
            }
            /*} catch (Exception e) {
                err = true;
                Console.WriteLine("unrecognized token: " + token + "\n");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return new Expr(Expr.E_VAL, 0);
            }*/
        }
    }
}
