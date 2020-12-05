using System;
using System.Drawing;

namespace Circuit.Elements {
    class VCCSElm : ChipElm {
        public bool broken;

        double gain;
        protected int inputCount;
        protected Expr expr;
        protected ExprState exprState;
        protected string exprString;
        double[] lastVolts;
        double lastvd;

        public VCCSElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f, st) {
            inputCount = st.nextTokenInt();
            exprString = CustomLogicModel.unescape(st.nextToken());
            parseExpr();
            setupPins();
        }

        public VCCSElm(int xx, int yy) : base(xx, yy) {
            inputCount = 2;
            exprString = ".1*(a-b)";
            parseExpr();
            setupPins();
        }

        public override int VoltageSourceCount { get { return 0; } }

        public override bool NonLinear { get { return true; } }

        public override int PostCount { get { return inputCount + 2; } }

        protected override string dump() {
            return base.dump() + " " + inputCount + " " + CustomLogicModel.escape(exprString);
        }

        protected override DUMP_ID getDumpType() { return DUMP_ID.VCCS; }

        public override void setupPins() {
            sizeX = 2;
            sizeY = inputCount > 2 ? inputCount : 2;
            pins = new Pin[inputCount + 2];
            for (int i = 0; i != inputCount; i++) {
                pins[i] = new Pin(this, i, SIDE_W, char.ToString((char)('A' + i)));
            }
            pins[inputCount] = new Pin(this, 0, SIDE_E, "C+");
            pins[inputCount + 1] = new Pin(this, 1, SIDE_E, "C-");
            lastVolts = new double[inputCount];
            exprState = new ExprState(inputCount);
        }

        string getChipName() { return "VCCS~"; } /* ~ is for localization */

        public override void Stamp() {
            Cir.StampNonLinear(Nodes[inputCount]);
            Cir.StampNonLinear(Nodes[inputCount + 1]);
        }

        protected double sign(double a, double b) {
            return a > 0 ? b : -b;
        }

        double getLimitStep() {
            /* get limit on changes in voltage per step.
             * be more lenient the more iterations we do */
            if (Cir.SubIterations < 4) {
                return 10;
            }
            if (Cir.SubIterations < 10) {
                return 1;
            }
            if (Cir.SubIterations < 20) {
                return .1;
            }
            if (Cir.SubIterations < 40) {
                return .01;
            }
            return .001;
        }

        protected double getConvergeLimit() {
            /* get maximum change in voltage per step when testing for convergence.
             * be more lenient over time */
            if (Cir.SubIterations < 10) {
                return .001;
            }
            if (Cir.SubIterations < 200) {
                return .01;
            }
            return .1;
        }

        public virtual bool hasCurrentOutput() { return true; }

        public int getOutputNode(int n) {
            return Nodes[n + inputCount];
        }

        public override void DoStep() {
            int i;

            /* no current path?  give up */
            if (broken) {
                pins[inputCount].current = 0;
                pins[inputCount + 1].current = 0;
                /* avoid singular matrix errors */
                Cir.StampResistor(Nodes[inputCount], Nodes[inputCount + 1], 1e8);
                return;
            }

            /* converged yet? */
            double limitStep = getLimitStep();
            double convergeLimit = getConvergeLimit();
            for (i = 0; i != inputCount; i++) {
                if (Math.Abs(Volts[i] - lastVolts[i]) > convergeLimit) {
                    Cir.Converged = false;
                }
                if (double.IsNaN(Volts[i])) {
                    Volts[i] = 0;
                }
                if (Math.Abs(Volts[i] - lastVolts[i]) > limitStep) {
                    Volts[i] = lastVolts[i] + sign(Volts[i] - lastVolts[i], limitStep);
                }
            }

            if (expr != null) {
                /* calculate output */
                for (i = 0; i != inputCount; i++) {
                    exprState.values[i] = Volts[i];
                }
                exprState.t = Sim.t;
                double v0 = -expr.eval(exprState);
                /*if (Math.Abs(volts[inputCount] - v0) > Math.Abs(v0) * .01 && cir.SubIterations < 100) {
                    cir.Converged = false;
                }*/
                double rs = v0;

                /* calculate and stamp output derivatives */
                for (i = 0; i != inputCount; i++) {
                    double dv = 1e-6;
                    exprState.values[i] = Volts[i] + dv;
                    double v = -expr.eval(exprState);
                    exprState.values[i] = Volts[i] - dv;
                    double v2 = -expr.eval(exprState);
                    double dx = (v - v2) / (dv * 2);
                    if (Math.Abs(dx) < 1e-6) {
                        dx = sign(dx, 1e-6);
                    }
                    Cir.StampVCCurrentSource(Nodes[inputCount], Nodes[inputCount + 1], Nodes[i], 0, dx);
                    /*Console.WriteLine("ccedx " + i + " " + dx); */
                    /* adjust right side */
                    rs -= dx * Volts[i];
                    exprState.values[i] = Volts[i];
                }
                /*Console.WriteLine("ccers " + rs);*/
                Cir.StampCurrentSource(Nodes[inputCount], Nodes[inputCount + 1], rs);
                pins[inputCount].current = -v0;
                pins[inputCount + 1].current = v0;
            }

            for (i = 0; i != inputCount; i++) {
                lastVolts[i] = Volts[i];
            }
        }

        public override void Draw(Graphics g) {
            drawChip(g);
        }

        public override bool GetConnection(int n1, int n2) {
            return comparePair(inputCount, inputCount + 1, n1, n2);
        }

        public override bool HasGroundConnection(int n1) {
            return false;
        }

        public override EditInfo GetEditInfo(int n) {
            if (n == 0) {
                var ei = new EditInfo(EditInfo.MakeLink("customfunction.html", "Output Function"), 0, -1, -1);
                ei.Text = exprString;
                ei.DisallowSliders();
                return ei;
            }
            if (n == 1) {
                return new EditInfo("# of Inputs", inputCount, 1, 8).SetDimensionless();
            }
            return null;
        }

        public override void SetEditValue(int n, EditInfo ei) {
            if (n == 0) {
                exprString = ei.Textf.Text.Replace(" ", "").Replace("\r", "").Replace("\n", "");
                parseExpr();
                return;
            }
            if (n == 1) {
                if (ei.Value < 0 || ei.Value > 8) {
                    return;
                }
                inputCount = (int)ei.Value;
                setupPins();
                allocNodes();
                SetPoints();
            }
        }

        public void setExpr(string expr) {
            exprString = expr.Replace(" ", "").Replace("\r", "").Replace("\n", "");
            parseExpr();
        }

        protected void parseExpr() {
            var parser = new ExprParser(exprString);
            expr = parser.parseExpression();
        }

        public override void GetInfo(string[] arr) {
            base.GetInfo(arr);
            int i;
            for (i = 0; arr[i] != null; i++) ;
            arr[i] = "I = " + getCurrentText(pins[inputCount].current);
        }
    }
}
