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

		public override bool has_connection(int n1, int n2) {
			if (ComparePair(0, 1, n1, n2)) {
				return true;
			}
			if (ComparePair(2, 3, n1, n2)) {
				return true;
			}
			return false;
		}

		public override void stamp() {
			/* voltage source (0V) between C+ and C- so we can measure current */
			int vn1 = Pins[1].voltSource;
			CircuitElement.StampVoltageSource(node_index[0], node_index[1], vn1, 0);

			CircuitElement.StampNonLinear(node_index[2]);
			CircuitElement.StampNonLinear(node_index[3]);
		}

		public override void do_iteration() {
			/* no current path?  give up */
			if (Broken) {
				Pins[InputCount].current = 0;
				Pins[InputCount + 1].current = 0;
				/* avoid singular matrix errors */
				CircuitElement.StampResistor(node_index[InputCount], node_index[InputCount + 1], 1e8);
				return;
			}

			/* converged yet?
             * double limitStep = getLimitStep()*.1; */
			var convergeLimit = GetConvergeLimit() * .1;

			var cur = Pins[1].current;
			if (Math.Abs(cur - mLastCurrent) > convergeLimit) {
				CircuitElement.converged = false;
			}
			int vn1 = CircuitElement.nodes.Length + Pins[1].voltSource;
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
			CircuitElement.StampCCCS(node_index[3], node_index[2], Pins[1].voltSource, dx);
			/* adjust right side */
			v0 -= dx * cur;
			/*Console.WriteLine("ccedx " + cur + " " + dx + " " + rs); */
			CircuitElement.StampCurrentSource(node_index[3], node_index[2], v0);
			mLastCurrent = cur;
		}

		public override void set_current(int vn, double c) {
			if (Pins[1].voltSource == vn) {
				Pins[0].current = -c;
				Pins[1].current = c;
			}
		}
	}
}
