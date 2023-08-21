using System;

using Circuit.Elements.Custom;
using Circuit.UI.Custom;

namespace Circuit.Elements.Input {
    class ElmVCCS : ElmChip {
        public bool mBroken;

        public int InputCount;
        public string ExprString;

        protected Expr mExpr;
        protected Expr.State mExprState;
        double[] mLastVolts;

        public ElmVCCS(Chip ui) : base() {
            InputCount = 2;
            ExprString = ".1*(a-b)";
            ParseExpr();
            SetupPins(ui);
        }

        public ElmVCCS(Chip ui, StringTokenizer st) : base(st) {
            InputCount = st.nextTokenInt(InputCount);
            if (st.nextToken(out ExprString, ExprString)) {
                ExprString = Utils.Unescape(ExprString);
                ParseExpr();
            }
            SetupPins(ui);
        }

        public override int AnaVoltageSourceCount { get { return 0; } }

        public override int PostCount { get { return InputCount + 2; } }

        public override void SetupPins(Chip ui) {
            ui.sizeX = 2;
            ui.sizeY = InputCount > 2 ? InputCount : 2;
            Pins = new Chip.Pin[InputCount + 2];
            for (int i = 0; i != InputCount; i++) {
                Pins[i] = new Chip.Pin(ui, i, Chip.SIDE_W, char.ToString((char)('A' + i)));
            }
            Pins[InputCount] = new Chip.Pin(ui, 0, Chip.SIDE_E, "C+");
            Pins[InputCount + 1] = new Chip.Pin(ui, 1, Chip.SIDE_E, "C-");
            mLastVolts = new double[InputCount];
            mExprState = new Expr.State(InputCount);
        }

        public override bool AnaGetConnection(int n1, int n2) {
            return ComparePair(InputCount, InputCount + 1, n1, n2);
        }

        public override bool AnaHasGroundConnection(int n1) {
            return false;
        }

        public override void AnaStamp() {
            Circuit.StampNonLinear(Nodes[InputCount]);
            Circuit.StampNonLinear(Nodes[InputCount + 1]);
        }

        public override void CirDoIteration() {
            int i;

            /* no current path?  give up */
            if (mBroken) {
                Pins[InputCount].current = 0;
                Pins[InputCount + 1].current = 0;
                /* avoid singular matrix errors */
                Circuit.StampResistor(Nodes[InputCount], Nodes[InputCount + 1], 1e8);
                return;
            }

            /* converged yet? */
            double limitStep = getLimitStep();
            double convergeLimit = getConvergeLimit();
            for (i = 0; i != InputCount; i++) {
                if (Math.Abs(Volts[i] - mLastVolts[i]) > convergeLimit) {
                    Circuit.Converged = false;
                }
                if (double.IsNaN(Volts[i])) {
                    Volts[i] = 0;
                }
                if (Math.Abs(Volts[i] - mLastVolts[i]) > limitStep) {
                    Volts[i] = mLastVolts[i] + sign(Volts[i] - mLastVolts[i], limitStep);
                }
            }

            if (mExpr != null) {
                /* calculate output */
                for (i = 0; i != InputCount; i++) {
                    mExprState.Values[i] = Volts[i];
                }
                mExprState.Time = Circuit.Time;
                double v0 = -mExpr.Eval(mExprState);
                /*if (Math.Abs(volts[inputCount] - v0) > Math.Abs(v0) * .01 && cir.SubIterations < 100) {
                    cir.Converged = false;
                }*/
                double rs = v0;

                /* calculate and stamp output derivatives */
                for (i = 0; i != InputCount; i++) {
                    double dv = 1e-6;
                    mExprState.Values[i] = Volts[i] + dv;
                    double v = -mExpr.Eval(mExprState);
                    mExprState.Values[i] = Volts[i] - dv;
                    double v2 = -mExpr.Eval(mExprState);
                    double dx = (v - v2) / (dv * 2);
                    if (Math.Abs(dx) < 1e-6) {
                        dx = sign(dx, 1e-6);
                    }
                    Circuit.StampVCCurrentSource(Nodes[InputCount], Nodes[InputCount + 1], Nodes[i], 0, dx);
                    /*Console.WriteLine("ccedx " + i + " " + dx); */
                    /* adjust right side */
                    rs -= dx * Volts[i];
                    mExprState.Values[i] = Volts[i];
                }
                /*Console.WriteLine("ccers " + rs);*/
                Circuit.StampCurrentSource(Nodes[InputCount], Nodes[InputCount + 1], rs);
                Pins[InputCount].current = -v0;
                Pins[InputCount + 1].current = v0;
            }

            for (i = 0; i != InputCount; i++) {
                mLastVolts[i] = Volts[i];
            }
        }

        protected double sign(double a, double b) {
            return a > 0 ? b : -b;
        }

        protected double getConvergeLimit() {
            /* get maximum change in voltage per step when testing for convergence.
             * be more lenient over time */
            if (Circuit.SubIterations < 10) {
                return 0.001;
            }
            if (Circuit.SubIterations < 200) {
                return 0.01;
            }
            return 0.1;
        }

        public virtual bool hasCurrentOutput() { return true; }

        double getLimitStep() {
            /* get limit on changes in voltage per step.
             * be more lenient the more iterations we do */
            if (Circuit.SubIterations < 4) {
                return 10;
            }
            if (Circuit.SubIterations < 10) {
                return 1;
            }
            if (Circuit.SubIterations < 20) {
                return 0.1;
            }
            if (Circuit.SubIterations < 40) {
                return 0.01;
            }
            return 0.001;
        }

        public void ParseExpr() {
            var parser = new Expr.Parser(ExprString);
            mExpr = parser.ParseExpression();
        }

        public void SetExpr(string expr) {
            ExprString = expr.Replace(" ", "").Replace("\r", "").Replace("\n", "");
            ParseExpr();
        }

        public int getOutputNode(int n) {
            return Nodes[n + InputCount];
        }
    }
}
