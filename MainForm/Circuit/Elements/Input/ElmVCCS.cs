using Circuit.Elements.Custom;
using Circuit.Symbol.Custom;

namespace Circuit.Elements.Input {
	class ElmVCCS : ElmChip {
		public delegate double DFunction(params double[] inputs);
		protected DFunction mFunction = (inputs) => 0;

		public bool Broken;
		public int InputCount;

		double[] mValues;
		double[] mLastVolts;

		public override int VoltageSourceCount { get { return 0; } }

		public override int TermCount { get { return InputCount + 2; } }

		public ElmVCCS() : base() {
			mFunction = (inputs) => {
				return 0.1 * (inputs[0] - inputs[1]);
			};
		}

		public override void SetupPins(Chip chip) {
			chip.sizeX = 2;
			chip.sizeY = InputCount > 2 ? InputCount : 2;
			Pins = new Chip.Pin[InputCount + 2];
			for (int i = 0; i != InputCount; i++) {
				Pins[i] = new Chip.Pin(chip, i, Chip.SIDE_W, char.ToString((char)('A' + i)));
			}
			Pins[InputCount] = new Chip.Pin(chip, 0, Chip.SIDE_E, "C+");
			Pins[InputCount + 1] = new Chip.Pin(chip, 1, Chip.SIDE_E, "C-");
			mLastVolts = new double[InputCount];
			mValues = new double[InputCount];
		}

		public int GetOutputNode(int n) {
			return NodeId[n + InputCount];
		}

		public void SetFunction(DFunction function) { mFunction = function; }

		protected double Sign(double a, double b) {
			return a > 0 ? b : -b;
		}

		protected double GetConvergeLimit() {
			/* get maximum change in voltage per step when testing for convergence.
             * be more lenient over time */
			if (CircuitElement.ITER_COUNT < 10) {
				return 0.001;
			}
			if (CircuitElement.ITER_COUNT < 200) {
				return 0.01;
			}
			return 0.1;
		}

		double getLimitStep() {
			/* get limit on changes in voltage per step.
             * be more lenient the more iterations we do */
			if (CircuitElement.ITER_COUNT < 4) {
				return 10;
			}
			if (CircuitElement.ITER_COUNT < 10) {
				return 1;
			}
			if (CircuitElement.ITER_COUNT < 20) {
				return 0.1;
			}
			if (CircuitElement.ITER_COUNT < 40) {
				return 0.01;
			}
			return 0.001;
		}

		#region [method(Analyze)]
		public override bool HasConnection(int n1, int n2) {
			return ComparePair(InputCount, InputCount + 1, n1, n2);
		}

		public override void Stamp() {
			StampNonLinear(NodeId[InputCount]);
			StampNonLinear(NodeId[InputCount + 1]);
		}
		#endregion

		#region [method(Circuit)]
		public override void DoIteration() {
			int i;

			/* no current path?  give up */
			if (Broken) {
				Pins[InputCount].current = 0;
				Pins[InputCount + 1].current = 0;
				/* avoid singular matrix errors */
				UpdateConductance(NodeId[InputCount], NodeId[InputCount + 1], 1e-8);
				return;
			}

			/* converged yet? */
			double limitStep = getLimitStep();
			double convergeLimit = GetConvergeLimit();
			for (i = 0; i != InputCount; i++) {
				if (Math.Abs(NodeVolts[i] - mLastVolts[i]) > convergeLimit) {
					CircuitState.Converged = false;
				}
				if (double.IsNaN(NodeVolts[i])) {
					NodeVolts[i] = 0;
				}
				if (Math.Abs(NodeVolts[i] - mLastVolts[i]) > limitStep) {
					NodeVolts[i] = mLastVolts[i] + Sign(NodeVolts[i] - mLastVolts[i], limitStep);
				}
			}

			/* calculate output */
			for (i = 0; i != InputCount; i++) {
				mValues[i] = NodeVolts[i];
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
				mValues[i] = NodeVolts[i] + dv;
				//var v1 = -mExpr.Eval(mExprState);
				var v1 = -mFunction(mValues);
				mValues[i] = NodeVolts[i] - dv;
				//var v2 = -mExpr.Eval(mExprState);
				var v2 = -mFunction(mValues);
				var dx = (v1 - v2) / (dv * 2);
				if (Math.Abs(dx) < 1e-6) {
					dx = Sign(dx, 1e-6);
				}
				StampVCCurrentSource(NodeId[InputCount], NodeId[InputCount + 1], NodeId[i], 0, dx);
				/*Console.WriteLine("ccedx " + i + " " + dx); */
				/* adjust right side */
				rs -= dx * NodeVolts[i];
				mValues[i] = NodeVolts[i];
			}
			/*Console.WriteLine("ccers " + rs);*/
			UpdateCurrent(NodeId[InputCount], NodeId[InputCount + 1], rs);
			Pins[InputCount].current = -v0;
			Pins[InputCount + 1].current = v0;

			for (i = 0; i != InputCount; i++) {
				mLastVolts[i] = NodeVolts[i];
			}
		}
		#endregion
	}
}
