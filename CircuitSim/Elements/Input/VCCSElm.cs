using System;
using System.Drawing;

using Circuit.Elements.Custom;

namespace Circuit.Elements.Input {
    class VCCSElm : ChipElm {
        public bool mBroken;

        protected int mInputCount;
        protected Expr mExpr;
        protected ExprState mExprState;
        protected string mExprString;
        double[] mLastVolts;

        public VCCSElm(Point p1, Point p2, int f, StringTokenizer st) : base(p1, p2, f, st) {
            mInputCount = st.nextTokenInt();
            mExprString = CustomLogicModel.unescape(st.nextToken());
            parseExpr();
            SetupPins();
        }

        public VCCSElm(Point pos) : base(pos) {
            mInputCount = 2;
            mExprString = ".1*(a-b)";
            parseExpr();
            SetupPins();
        }

        public override int VoltageSourceCount { get { return 0; } }

        public override bool NonLinear { get { return true; } }

        public override int PostCount { get { return mInputCount + 2; } }

        public override DUMP_ID DumpType { get { return DUMP_ID.VCCS; } }

        public virtual bool hasCurrentOutput() { return true; }

        protected override string dump() {
            return base.dump() + " " + mInputCount + " " + CustomLogicModel.escape(mExprString);
        }

        protected double sign(double a, double b) {
            return a > 0 ? b : -b;
        }

        protected void parseExpr() {
            var parser = new ExprParser(mExprString);
            mExpr = parser.ParseExpression();
        }

        protected double getConvergeLimit() {
            /* get maximum change in voltage per step when testing for convergence.
             * be more lenient over time */
            if (mCir.SubIterations < 10) {
                return .001;
            }
            if (mCir.SubIterations < 200) {
                return .01;
            }
            return .1;
        }

        public override bool GetConnection(int n1, int n2) {
            return comparePair(mInputCount, mInputCount + 1, n1, n2);
        }

        public override bool HasGroundConnection(int n1) {
            return false;
        }

        public override void SetupPins() {
            sizeX = 2;
            sizeY = mInputCount > 2 ? mInputCount : 2;
            pins = new Pin[mInputCount + 2];
            for (int i = 0; i != mInputCount; i++) {
                pins[i] = new Pin(this, i, SIDE_W, char.ToString((char)('A' + i)));
            }
            pins[mInputCount] = new Pin(this, 0, SIDE_E, "C+");
            pins[mInputCount + 1] = new Pin(this, 1, SIDE_E, "C-");
            mLastVolts = new double[mInputCount];
            mExprState = new ExprState(mInputCount);
        }

        string getChipName() { return "VCCS~"; } /* ~ is for localization */

        public void SetExpr(string expr) {
            mExprString = expr.Replace(" ", "").Replace("\r", "").Replace("\n", "");
            parseExpr();
        }

        public override void Stamp() {
            mCir.StampNonLinear(Nodes[mInputCount]);
            mCir.StampNonLinear(Nodes[mInputCount + 1]);
        }

        public override void DoStep() {
            int i;

            /* no current path?  give up */
            if (mBroken) {
                pins[mInputCount].current = 0;
                pins[mInputCount + 1].current = 0;
                /* avoid singular matrix errors */
                mCir.StampResistor(Nodes[mInputCount], Nodes[mInputCount + 1], 1e8);
                return;
            }

            /* converged yet? */
            double limitStep = getLimitStep();
            double convergeLimit = getConvergeLimit();
            for (i = 0; i != mInputCount; i++) {
                if (Math.Abs(Volts[i] - mLastVolts[i]) > convergeLimit) {
                    mCir.Converged = false;
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
                for (i = 0; i != mInputCount; i++) {
                    mExprState.Values[i] = Volts[i];
                }
                mExprState.Time = CirSim.Sim.Time;
                double v0 = -mExpr.Eval(mExprState);
                /*if (Math.Abs(volts[inputCount] - v0) > Math.Abs(v0) * .01 && cir.SubIterations < 100) {
                    cir.Converged = false;
                }*/
                double rs = v0;

                /* calculate and stamp output derivatives */
                for (i = 0; i != mInputCount; i++) {
                    double dv = 1e-6;
                    mExprState.Values[i] = Volts[i] + dv;
                    double v = -mExpr.Eval(mExprState);
                    mExprState.Values[i] = Volts[i] - dv;
                    double v2 = -mExpr.Eval(mExprState);
                    double dx = (v - v2) / (dv * 2);
                    if (Math.Abs(dx) < 1e-6) {
                        dx = sign(dx, 1e-6);
                    }
                    mCir.StampVCCurrentSource(Nodes[mInputCount], Nodes[mInputCount + 1], Nodes[i], 0, dx);
                    /*Console.WriteLine("ccedx " + i + " " + dx); */
                    /* adjust right side */
                    rs -= dx * Volts[i];
                    mExprState.Values[i] = Volts[i];
                }
                /*Console.WriteLine("ccers " + rs);*/
                mCir.StampCurrentSource(Nodes[mInputCount], Nodes[mInputCount + 1], rs);
                pins[mInputCount].current = -v0;
                pins[mInputCount + 1].current = v0;
            }

            for (i = 0; i != mInputCount; i++) {
                mLastVolts[i] = Volts[i];
            }
        }

        double getLimitStep() {
            /* get limit on changes in voltage per step.
             * be more lenient the more iterations we do */
            if (mCir.SubIterations < 4) {
                return 10;
            }
            if (mCir.SubIterations < 10) {
                return 1;
            }
            if (mCir.SubIterations < 20) {
                return .1;
            }
            if (mCir.SubIterations < 40) {
                return .01;
            }
            return .001;
        }

        public int getOutputNode(int n) {
            return Nodes[n + mInputCount];
        }

        public override void Draw(CustomGraphics g) {
            drawChip(g);
        }

        public override void GetInfo(string[] arr) {
            base.GetInfo(arr);
            int i;
            for (i = 0; arr[i] != null; i++)
                ;
            arr[i] = "I = " + Utils.CurrentText(pins[mInputCount].current);
        }

        public override ElementInfo GetElementInfo(int n) {
            if (n == 0) {
                var ei = new ElementInfo(ElementInfo.MakeLink("customfunction.html", "Output Function"), 0, -1, -1);
                ei.Text = mExprString;
                ei.DisallowSliders();
                return ei;
            }
            if (n == 1) {
                return new ElementInfo("入力数", mInputCount, 1, 8).SetDimensionless();
            }
            return null;
        }

        public override void SetElementValue(int n, ElementInfo ei) {
            if (n == 0) {
                mExprString = ei.Textf.Text.Replace(" ", "").Replace("\r", "").Replace("\n", "");
                parseExpr();
                return;
            }
            if (n == 1) {
                if (ei.Value < 0 || ei.Value > 8) {
                    return;
                }
                mInputCount = (int)ei.Value;
                SetupPins();
                allocNodes();
                SetPoints();
            }
        }
    }
}
