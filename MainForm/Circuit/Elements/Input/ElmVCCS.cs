﻿using Circuit.Elements.Custom;
using Circuit.Symbol.Custom;

namespace Circuit.Elements.Input {
	class ElmVCCS : ElmChip {
		public delegate double DFunction(params double[] inputs);
		protected DFunction mFunction = (inputs) => 0;

		public bool Broken;
		public int InputCount;

		double[] mValues;
		double[] mLastVolts;

		public ElmVCCS(Chip ui) : base() {
			InputCount = 2;
			mFunction = (inputs) => {
				return 0.1 * (inputs[0] - inputs[1]);
			};
			SetupPins(ui);
		}

		public ElmVCCS(Chip ui, StringTokenizer st) : base(st) {
			InputCount = st.nextTokenInt(InputCount);
			mFunction = (inputs) => {
				return 0.1 * (inputs[0] - inputs[1]);
			};
			SetupPins(ui);
		}

		public override int VoltageSourceCount { get { return 0; } }

		public override int TermCount { get { return InputCount + 2; } }

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
			mValues = new double[InputCount];
		}

		public override bool GetConnection(int n1, int n2) {
			return ComparePair(InputCount, InputCount + 1, n1, n2);
		}

		public override bool HasGroundConnection(int n1) {
			return false;
		}

		public override void Stamp() {
			CircuitElement.StampNonLinear(Nodes[InputCount]);
			CircuitElement.StampNonLinear(Nodes[InputCount + 1]);
		}

		public override void DoIteration() {
			int i;

			/* no current path?  give up */
			if (Broken) {
				Pins[InputCount].current = 0;
				Pins[InputCount + 1].current = 0;
				/* avoid singular matrix errors */
				CircuitElement.StampResistor(Nodes[InputCount], Nodes[InputCount + 1], 1e8);
				return;
			}

			/* converged yet? */
			double limitStep = getLimitStep();
			double convergeLimit = getConvergeLimit();
			for (i = 0; i != InputCount; i++) {
				if (Math.Abs(Volts[i] - mLastVolts[i]) > convergeLimit) {
					CircuitElement.Converged = false;
				}
				if (double.IsNaN(Volts[i])) {
					Volts[i] = 0;
				}
				if (Math.Abs(Volts[i] - mLastVolts[i]) > limitStep) {
					Volts[i] = mLastVolts[i] + sign(Volts[i] - mLastVolts[i], limitStep);
				}
			}

			/* calculate output */
			for (i = 0; i != InputCount; i++) {
				mValues[i] = Volts[i];
			}
			//mValues.Time = Circuit.Time;
			//var v0 = -mExpr.Eval(mExprState);
			var v0 = -mFunction(mValues);
			/*if (Math.Abs(volts[inputCount] - v0) > Math.Abs(v0) * .01 && cir.SubIterations < 100) {
				cir.Converged = false;
			}*/
			var rs = v0;

			/* calculate and stamp output derivatives */
			for (i = 0; i != InputCount; i++) {
				var dv = 1e-6;
				mValues[i] = Volts[i] + dv;
				//var v1 = -mExpr.Eval(mExprState);
				var v1 = -mFunction(mValues);
				mValues[i] = Volts[i] - dv;
				//var v2 = -mExpr.Eval(mExprState);
				var v2 = -mFunction(mValues);
				var dx = (v1 - v2) / (dv * 2);
				if (Math.Abs(dx) < 1e-6) {
					dx = sign(dx, 1e-6);
				}
				CircuitElement.StampVCCurrentSource(Nodes[InputCount], Nodes[InputCount + 1], Nodes[i], 0, dx);
				/*Console.WriteLine("ccedx " + i + " " + dx); */
				/* adjust right side */
				rs -= dx * Volts[i];
				mValues[i] = Volts[i];
			}
			/*Console.WriteLine("ccers " + rs);*/
			CircuitElement.StampCurrentSource(Nodes[InputCount], Nodes[InputCount + 1], rs);
			Pins[InputCount].current = -v0;
			Pins[InputCount + 1].current = v0;

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
			if (CircuitElement.SubIterations < 10) {
				return 0.001;
			}
			if (CircuitElement.SubIterations < 200) {
				return 0.01;
			}
			return 0.1;
		}

		public virtual bool hasCurrentOutput() { return true; }

		public void SetFunction(DFunction function) { mFunction = function; }

		double getLimitStep() {
			/* get limit on changes in voltage per step.
             * be more lenient the more iterations we do */
			if (CircuitElement.SubIterations < 4) {
				return 10;
			}
			if (CircuitElement.SubIterations < 10) {
				return 1;
			}
			if (CircuitElement.SubIterations < 20) {
				return 0.1;
			}
			if (CircuitElement.SubIterations < 40) {
				return 0.01;
			}
			return 0.001;
		}

		public int getOutputNode(int n) {
			return Nodes[n + InputCount];
		}
	}
}
