using System;
using System.Drawing;

namespace Circuit.Elements {
    class CCCSElm : VCCSElm {
        double lastCurrent;

        public CCCSElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f, st) {
            inputCount = 2;
            SetupPins();
        }

        public CCCSElm(Point pos) : base(pos) {
            exprString = "2*i";
            parseExpr();
        }

        public override int VoltageSourceCount { get { return 1; } }

        public override int PostCount { get { return 4; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.CCCS; } }

        public override void SetupPins() {
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
            mCir.StampVoltageSource(Nodes[0], Nodes[1], vn1, 0);

            mCir.StampNonLinear(Nodes[2]);
            mCir.StampNonLinear(Nodes[3]);
        }

        public override void DoStep() {
            /* no current path?  give up */
            if (broken) {
                pins[inputCount].current = 0;
                pins[inputCount + 1].current = 0;
                /* avoid singular matrix errors */
                mCir.StampResistor(Nodes[inputCount], Nodes[inputCount + 1], 1e8);
                return;
            }

            /* converged yet?
             * double limitStep = getLimitStep()*.1; */
            double convergeLimit = getConvergeLimit() * .1;

            double cur = pins[1].current;
            if (Math.Abs(cur - lastCurrent) > convergeLimit) {
                mCir.Converged = false;
            }
            int vn1 = pins[1].voltSource + mCir.NodeList.Count;
            if (expr != null) {
                /* calculate output */
                exprState.values[8] = cur;  /* I = current */
                exprState.t = Sim.Time;
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
                mCir.StampCCCS(Nodes[3], Nodes[2], pins[1].voltSource, dx);
                /* adjust right side */
                rs -= dx * cur;
                /*Console.WriteLine("ccedx " + cur + " " + dx + " " + rs); */
                mCir.StampCurrentSource(Nodes[3], Nodes[2], rs);
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

        public override ElementInfo GetElementInfo(int n) {
            /* can't set number of inputs */
            if (n == 1) {
                return null;
            }
            return base.GetElementInfo(n);
        }
    }
}
