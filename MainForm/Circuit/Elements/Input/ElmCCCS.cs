using Circuit.Symbol.Custom;

namespace Circuit.Elements.Input {
	class ElmCCCS : ElmVCCS {
		double mLastCurrent;

		public override int VoltageSourceCount { get { return 1; } }

		public override int TermCount { get { return 4; } }

		public ElmCCCS() : base() { }

		public override void SetupPins(Chip ui) {
			ui.sizeX = 2;
			ui.sizeY = 2;
			Pins = new Chip.Pin[4];
			Pins[0] = new Chip.Pin(ui, 0, Chip.SIDE_W, "C+");
			Pins[1] = new Chip.Pin(ui, 1, Chip.SIDE_W, "C-") {
				output = true
			};
			Pins[2] = new Chip.Pin(ui, 0, Chip.SIDE_E, "O+") {
				output = true
			};
			Pins[3] = new Chip.Pin(ui, 1, Chip.SIDE_E, "O-");
		}

		public override bool HasConnection(int n1, int n2) {
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
			StampVoltageSource(NodeId[0], NodeId[1], vn1, 0);

			StampNonLinear(NodeId[2]);
			StampNonLinear(NodeId[3]);
		}

		public override void DoIteration() {
			/* no current path?  give up */
			if (Broken) {
				Pins[InputCount].current = 0;
				Pins[InputCount + 1].current = 0;
				/* avoid singular matrix errors */
				UpdateConductance(NodeId[InputCount], NodeId[InputCount + 1], 1e-8);
				return;
			}

			/* converged yet?
             * double limitStep = getLimitStep()*.1; */
			var convergeLimit = GetConvergeLimit() * .1;

			var cur = Pins[1].current;
			if (Math.Abs(cur - mLastCurrent) > convergeLimit) {
				CircuitState.Converged = false;
			}
			/* calculate output */
			var v0 = mFunction(cur);
			Pins[2].current = v0;
			Pins[3].current = -v0;
			var dv = 1e-6;
			var v1 = mFunction(cur + dv);
			var v2 = mFunction(cur - dv);
			var dx = (v1 - v2) / (dv * 2);
			if (Math.Abs(dx) < 1e-6) {
				dx = Sign(dx, 1e-6);
			}
			StampCCCS(NodeId[3], NodeId[2], Pins[1].voltSource, dx);
			/* adjust right side */
			v0 -= dx * cur;
			/*Console.WriteLine("ccedx " + cur + " " + dx + " " + rs); */
			UpdateCurrent(NodeId[3], NodeId[2], v0);
			mLastCurrent = cur;
		}

		public override void SetCurrent(int vn, double c) {
			if (Pins[1].voltSource == vn) {
				Pins[0].current = -c;
				Pins[1].current = c;
			}
		}

		/* stamp a current source from n1 to n2 depending on current through voltage_source */
		private static void StampCCCS(int n1, int n2, int voltage_source, double gain) {
			var vn = CircuitAnalizer.NodeCount + voltage_source;
			StampMatrix(n1, vn, gain);
			StampMatrix(n2, vn, -gain);
		}
	}
}
