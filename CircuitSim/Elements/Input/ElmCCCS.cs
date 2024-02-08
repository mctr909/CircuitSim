using System;

using Circuit.Symbol.Input;
using Circuit.Symbol.Custom;

namespace Circuit.Elements.Input {
	class ElmCCCS : ElmVCCS {
		double mLastCurrent;

		public ElmCCCS(VCCS ui) : base(ui) {
			ExprString = "2*i";
			ParseExpr();
		}

		public ElmCCCS(VCCS ui, StringTokenizer st) : base(ui, st) {
			InputCount = 2;
			SetupPins(ui);
		}

		public override int VoltageSourceCount { get { return 1; } }

		public override int TermCount { get { return 4; } }

		public override void SetupPins(Chip ui) {
			ui.sizeX = 2;
			ui.sizeY = 2;
			Pins = new Chip.Pin[4];
			Pins[0] = new Chip.Pin(ui, 0, Chip.SIDE_W, "C+");
			Pins[1] = new Chip.Pin(ui, 1, Chip.SIDE_W, "C-");
			Pins[1].output = true;
			Pins[2] = new Chip.Pin(ui, 0, Chip.SIDE_E, "O+");
			Pins[2].output = true;
			Pins[3] = new Chip.Pin(ui, 1, Chip.SIDE_E, "O-");
			mExprState = new Expr.State(1);
		}

		public override bool hasCurrentOutput() { return true; }

		public override bool GetConnection(int n1, int n2) {
			if (ComparePair(0, 1, n1, n2)) {
				return true;
			}
			if (ComparePair(2, 3, n1, n2)) {
				return true;
			}
			return false;
		}

		public override void Stamp() {
			/* voltage source (0V) between C+ and C- so we can measure current */
			int vn1 = Pins[1].voltSource;
			Circuit.StampVoltageSource(Nodes[0], Nodes[1], vn1, 0);

			Circuit.StampNonLinear(Nodes[2]);
			Circuit.StampNonLinear(Nodes[3]);
		}

		public override void DoIteration() {
			/* no current path?  give up */
			if (mBroken) {
				Pins[InputCount].current = 0;
				Pins[InputCount + 1].current = 0;
				/* avoid singular matrix errors */
				Circuit.StampResistor(Nodes[InputCount], Nodes[InputCount + 1], 1e8);
				return;
			}

			/* converged yet?
             * double limitStep = getLimitStep()*.1; */
			double convergeLimit = getConvergeLimit() * .1;

			double cur = Pins[1].current;
			if (Math.Abs(cur - mLastCurrent) > convergeLimit) {
				Circuit.Converged = false;
			}
			int vn1 = Pins[1].voltSource + Circuit.Nodes.Count;
			if (mExpr != null) {
				/* calculate output */
				mExprState.Values[8] = cur;  /* I = current */
				mExprState.Time = Circuit.Time;
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
				Circuit.StampCCCS(Nodes[3], Nodes[2], Pins[1].voltSource, dx);
				/* adjust right side */
				rs -= dx * cur;
				/*Console.WriteLine("ccedx " + cur + " " + dx + " " + rs); */
				Circuit.StampCurrentSource(Nodes[3], Nodes[2], rs);
			}

			mLastCurrent = cur;
		}

		public override void SetCurrent(int vn, double c) {
			if (Pins[1].voltSource == vn) {
				Pins[0].current = -c;
				Pins[1].current = c;
			}
		}
	}
}
