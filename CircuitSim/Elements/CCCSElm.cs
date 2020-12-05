using System;

namespace Circuit.Elements {
    class CCCSElm : VCCSElm {
        double lastCurrent;

        public CCCSElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f, st) {
            inputCount = 2;
            setupPins();
        }

        public CCCSElm(int xx, int yy) : base(xx, yy) {
            exprString = "2*i";
            parseExpr();
        }

        public override int VoltageSourceCount { get { return 1; } }

        public override int PostCount { get { return 4; } }

        protected override DUMP_ID getDumpType() { return DUMP_ID.CCCS; }

        public override void setupPins() {
            sizeX = 2;
            sizeY = 2;
            pins = new Pin[4];
            pins[0] = new Pin(this, 0, SIDE_W, "C+");
            pins[1] = new Pin(this, 1, SIDE_W, "C-");
            pins[1].output = true;
            pins[2] = new Pin(this, 0, SIDE_E, "O+");
            pins[2].output = true;
            pins[3] = new Pin(this, 1, SIDE_E, "O-");
            exprState = new ExprState(1);
        }

        string getChipName() { return "CCCS"; }

        public override void Stamp() {
            /* voltage source (0V) between C+ and C- so we can measure current */
            int vn1 = pins[1].voltSource;
            Cir.StampVoltageSource(Nodes[0], Nodes[1], vn1, 0);

            Cir.StampNonLinear(Nodes[2]);
            Cir.StampNonLinear(Nodes[3]);
        }

        public override void DoStep() {
            /* no current path?  give up */
            if (broken) {
                pins[inputCount].current = 0;
                pins[inputCount + 1].current = 0;
                /* avoid singular matrix errors */
                Cir.StampResistor(Nodes[inputCount], Nodes[inputCount + 1], 1e8);
                return;
            }

            /* converged yet?
             * double limitStep = getLimitStep()*.1; */
            double convergeLimit = getConvergeLimit() * .1;

            double cur = pins[1].current;
            if (Math.Abs(cur - lastCurrent) > convergeLimit) {
                Cir.Converged = false;
            }
            int vn1 = pins[1].voltSource + Cir.NodeList.Count;
            if (expr != null) {
                /* calculate output */
                exprState.values[8] = cur;  /* I = current */
                exprState.t = Sim.t;
                double v0 = expr.eval(exprState);
                double rs = v0;
                pins[2].current = v0;
                pins[3].current = -v0;

                double dv = 1e-6;
                exprState.values[8] = cur + dv;
                double v = expr.eval(exprState);
                exprState.values[8] = cur - dv;
                double v2 = expr.eval(exprState);
                double dx = (v - v2) / (dv * 2);
                if (Math.Abs(dx) < 1e-6) {
                    dx = sign(dx, 1e-6);
                }
                Cir.StampCCCS(Nodes[3], Nodes[2], pins[1].voltSource, dx);
                /* adjust right side */
                rs -= dx * cur;
                /*Console.WriteLine("ccedx " + cur + " " + dx + " " + rs); */
                Cir.StampCurrentSource(Nodes[3], Nodes[2], rs);
            }

            lastCurrent = cur;
        }

        public override bool GetConnection(int n1, int n2) {
            if (comparePair(0, 1, n1, n2)) {
                return true;
            }
            if (comparePair(2, 3, n1, n2)) {
                return true;
            }
            return false;
        }

        public override bool hasCurrentOutput() { return true; }

        public override void SetCurrent(int vn, double c) {
            if (pins[1].voltSource == vn) {
                pins[0].current = -c;
                pins[1].current = c;
            }
        }

        public override EditInfo GetEditInfo(int n) {
            /* can't set number of inputs */
            if (n == 1) {
                return null;
            }
            return base.GetEditInfo(n);
        }
    }
}
