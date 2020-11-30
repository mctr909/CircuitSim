using System;
using System.Drawing;

namespace Circuit.Elements {
    class VCCSElm : ChipElm {
        public bool broken;

        double gain;
        int inputCount;
        Expr expr;
        ExprState exprState;
        string exprString;
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

        public override string dump() {
            return base.dump() + " " + inputCount + " " + CustomLogicModel.escape(exprString);
        }

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

        public override bool nonLinear() { return true; }

        public override void stamp() {
            cir.StampNonLinear(Nodes[inputCount]);
            cir.StampNonLinear(Nodes[inputCount + 1]);
        }

        double sign(double a, double b) {
            return a > 0 ? b : -b;
        }

        double getLimitStep() {
            /* get limit on changes in voltage per step.
             * be more lenient the more iterations we do */
            if (cir.SubIterations < 4) {
                return 10;
            }
            if (cir.SubIterations < 10) {
                return 1;
            }
            if (cir.SubIterations < 20) {
                return .1;
            }
            if (cir.SubIterations < 40) {
                return .01;
            }
            return .001;
        }

        double getConvergeLimit() {
            /* get maximum change in voltage per step when testing for convergence.
             * be more lenient over time */
            if (cir.SubIterations < 10) {
                return .001;
            }
            if (cir.SubIterations < 200) {
                return .01;
            }
            return .1;
        }

        public bool hasCurrentOutput() { return true; }

        public int getOutputNode(int n) {
            return Nodes[n + inputCount];
        }

        public override void doStep() {
            int i;

            /* no current path?  give up */
            if (broken) {
                pins[inputCount].current = 0;
                pins[inputCount + 1].current = 0;
                /* avoid singular matrix errors */
                cir.StampResistor(Nodes[inputCount], Nodes[inputCount + 1], 1e8);
                return;
            }

            /* converged yet? */
            double limitStep = getLimitStep();
            double convergeLimit = getConvergeLimit();
            for (i = 0; i != inputCount; i++) {
                if (Math.Abs(Volts[i] - lastVolts[i]) > convergeLimit) {
                    cir.Converged = false;
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
                exprState.t = sim.t;
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
                    cir.StampVCCurrentSource(Nodes[inputCount], Nodes[inputCount + 1], Nodes[i], 0, dx);
                    /*Console.WriteLine("ccedx " + i + " " + dx); */
                    /* adjust right side */
                    rs -= dx * Volts[i];
                    exprState.values[i] = Volts[i];
                }
                /*Console.WriteLine("ccers " + rs);*/
                cir.StampCurrentSource(Nodes[inputCount], Nodes[inputCount + 1], rs);
                pins[inputCount].current = -v0;
                pins[inputCount + 1].current = v0;
            }

            for (i = 0; i != inputCount; i++) {
                lastVolts[i] = Volts[i];
            }
        }

        public override void draw(Graphics g) {
            drawChip(g);
        }

        public override int getPostCount() { return inputCount + 2; }

        public override int getVoltageSourceCount() { return 0; }

        public override DUMP_ID getDumpType() { return DUMP_ID.VCCS; }

        public override bool getConnection(int n1, int n2) {
            return comparePair(inputCount, inputCount + 1, n1, n2);
        }

        public override bool hasGroundConnection(int n1) {
            return false;
        }

        public override EditInfo getEditInfo(int n) {
            if (n == 0) {
                var ei = new EditInfo(EditInfo.makeLink("customfunction.html", "Output Function"), 0, -1, -1);
                ei.text = exprString;
                ei.disallowSliders();
                return ei;
            }
            if (n == 1) {
                return new EditInfo("# of Inputs", inputCount, 1, 8).setDimensionless();
            }
            return null;
        }

        public override void setEditValue(int n, EditInfo ei) {
            if (n == 0) {
                exprString = ei.textf.Text;
                parseExpr();
                return;
            }
            if (n == 1) {
                if (ei.value < 0 || ei.value > 8) {
                    return;
                }
                inputCount = (int)ei.value;
                setupPins();
                allocNodes();
                setPoints();
            }
        }

        void setExpr(string expr) {
            exprString = expr;
            parseExpr();
        }

        void parseExpr() {
            var parser = new ExprParser(exprString);
            expr = parser.parseExpression();
        }

        public override void getInfo(string[] arr) {
            base.getInfo(arr);
            int i;
            for (i = 0; arr[i] != null; i++) ;
            arr[i] = "I = " + getCurrentText(pins[inputCount].current);
        }
    }
}
