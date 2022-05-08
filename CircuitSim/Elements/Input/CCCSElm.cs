using System;

using Circuit.Elements.Custom;

namespace Circuit.Elements.Input {
    class CCCSElm : VCCSElm {
        double mLastCurrent;

        public CCCSElm(VCCSUI ui) : base(ui) {
            ExprString = "2*i";
            ParseExpr();
        }

        public CCCSElm(VCCSUI ui, StringTokenizer st) : base(ui, st) {
            InputCount = 2;
            SetupPins(ui);
        }

        public override int VoltageSourceCount { get { return 1; } }

        public override int PostCount { get { return 4; } }

        public override void SetupPins(ChipUI ui) {
            ui.sizeX = 2;
            ui.sizeY = 2;
            Pins = new ChipUI.Pin[4];
            Pins[0] = new ChipUI.Pin(ui, 0, ChipUI.SIDE_W, "C+");
            Pins[1] = new ChipUI.Pin(ui, 1, ChipUI.SIDE_W, "C-");
            Pins[1].output = true;
            Pins[2] = new ChipUI.Pin(ui, 0, ChipUI.SIDE_E, "O+");
            Pins[2].output = true;
            Pins[3] = new ChipUI.Pin(ui, 1, ChipUI.SIDE_E, "O-");
            mExprState = new ExprState(1);
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

        public override void AnaStamp() {
            /* voltage source (0V) between C+ and C- so we can measure current */
            int vn1 = Pins[1].voltSource;
            mCir.StampVoltageSource(Nodes[0], Nodes[1], vn1, 0);

            mCir.StampNonLinear(Nodes[2]);
            mCir.StampNonLinear(Nodes[3]);
        }

        public override void CirDoStep() {
            /* no current path?  give up */
            if (mBroken) {
                Pins[InputCount].current = 0;
                Pins[InputCount + 1].current = 0;
                /* avoid singular matrix errors */
                mCir.StampResistor(Nodes[InputCount], Nodes[InputCount + 1], 1e8);
                return;
            }

            /* converged yet?
             * double limitStep = getLimitStep()*.1; */
            double convergeLimit = getConvergeLimit() * .1;

            double cur = Pins[1].current;
            if (Math.Abs(cur - mLastCurrent) > convergeLimit) {
                mCir.Converged = false;
            }
            int vn1 = Pins[1].voltSource + mCir.NodeList.Count;
            if (mExpr != null) {
                /* calculate output */
                mExprState.Values[8] = cur;  /* I = current */
                mExprState.Time = CirSim.Sim.Time;
                double v0 = mExpr.Eval(mExprState);
                double rs = v0;
                Pins[2].current = v0;
                Pins[3].current = -v0;

                double dv = 1e-6;
                mExprState.Values[8] = cur + dv;
                double v = mExpr.Eval(mExprState);
                mExprState.Values[8] = cur - dv;
                double v2 = mExpr.Eval(mExprState);
                double dx = (v - v2) / (dv * 2);
                if (Math.Abs(dx) < 1e-6) {
                    dx = sign(dx, 1e-6);
                }
                mCir.StampCCCS(Nodes[3], Nodes[2], Pins[1].voltSource, dx);
                /* adjust right side */
                rs -= dx * cur;
                /*Console.WriteLine("ccedx " + cur + " " + dx + " " + rs); */
                mCir.StampCurrentSource(Nodes[3], Nodes[2], rs);
            }

            mLastCurrent = cur;
        }

        public override void CirSetCurrent(int vn, double c) {
            if (Pins[1].voltSource == vn) {
                Pins[0].current = -c;
                Pins[1].current = c;
            }
        }
    }
}
