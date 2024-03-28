namespace MainForm {
	internal class ScopePlot {
		public double[] minValues, maxValues;
		int scopePointCount;
		public int ptr; // ptr is pointer to the current sample
		public int value; // Value - the property being shown - e.g. VAL_CURRENT
				   // scopePlotSpeed is in sim timestep units per pixel
		public int scopePlotSpeed, units;
		double lastUpdateTime;
		public double lastValue;
		public Color color;
		public ElmBase elm;
		// Has a manual scale in "/div" format been put in by the user (as opposed to
		// being
		// inferred from a "MaxValue" format or from an automatically calculated scale)?
		// Manual scales should be kept to sane values anyway, but this shows if this is
		// a user
		// intention we should respect, or if we should try and populate reasonable
		// values from
		// the data we have
		public bool manScaleSet = false;
		public double manScale = 1.0; // Units per division
		public int manVPosition = 0; // 0 is center of screen. +V_POSITION_STEPS/2 is top of screen
		public double gridMult;
		public double plotOffset;
		public bool acCoupled = false;
		double acAlpha = 0.9999; // Filter coefficient for AC coupling
		double acLastOut = 0; // Store y[i-1] term for AC coupling filter

		public const int FLAG_AC = 1;

		public ScopePlot(ElmBase e, int u) {
			elm = e;
			units = u;
		}

		public ScopePlot(ElmBase e, int u, int v, double manS) {
			elm = e;
			units = u;
			value = v;
			manScale = manS;
			// I *think* all units other than V and A can only be positive, so for these
			// move the v position to the bottom.
			if (units > Scope.UNITS_A)
				manVPosition = -Scope.V_POSITION_STEPS / 2;
		}

		public int startIndex(int w) {
			return ptr + scopePointCount - w;
		}

		public void reset(int spc, int sp, bool full) {
			var oldSpc = scopePointCount;
			scopePointCount = spc;
			if (scopePlotSpeed != sp)
				oldSpc = 0; // throw away old data
			scopePlotSpeed = sp;
			// Adjust the time constant of the AC coupled filter in proportion to the number
			// of samples
			// we are seeing on the scope (if my maths is right). The constant is
			// empirically determined
			acAlpha = 1.0 - 1.0 / (1.15 * scopePlotSpeed * scopePointCount);
			var oldMin = minValues;
			var oldMax = maxValues;
			minValues = new double[scopePointCount];
			maxValues = new double[scopePointCount];
			if (oldMin != null && !full) {
				// preserve old data if possible
				int i;
				for (i = 0; i != scopePointCount && i != oldSpc; i++) {
					var i1 = (-i) & (scopePointCount - 1);
					var i2 = (ptr - i) & (oldSpc - 1);
					minValues[i1] = oldMin[i2];
					maxValues[i1] = oldMax[i2];
				}
			} else
				lastUpdateTime = CirSim.theSim.t;
			ptr = 0;
		}

		public void timeStep() {
			if (elm == null)
				return;
			var v = elm.getScopeValue(value);
			// AC coupling filter. 1st order IIR high pass
			// y[i] = alpha x (y[i-1]+x[i]-x[i-1])
			// We calculate for all iterations (even DC coupled) to prime the data in case
			// they switch to AC later
			var newAcOut = acAlpha * (acLastOut + v - lastValue);
			lastValue = v;
			acLastOut = newAcOut;
			if (isAcCoupled())
				v = newAcOut;
			if (v < minValues[ptr])
				minValues[ptr] = v;
			if (v > maxValues[ptr])
				maxValues[ptr] = v;
			if (CirSim.theSim.t - lastUpdateTime >= CirSim.theSim.maxTimeStep * scopePlotSpeed) {
				ptr = (ptr + 1) & (scopePointCount - 1);
				minValues[ptr] = maxValues[ptr] = v;
				lastUpdateTime += CirSim.theSim.maxTimeStep * scopePlotSpeed;
			}
		}

		public string getUnitText(double v) {
			switch (units) {
			case Scope.UNITS_V:
				return ElmBase.getVoltageText(v);
			case Scope.UNITS_A:
				return ElmBase.getCurrentText(v);
			case Scope.UNITS_OHMS:
				return ElmBase.getUnitText(v, "Ω");
			case Scope.UNITS_W:
				return ElmBase.getUnitText(v, "W");
			}
			return null;
		}

		static readonly Color[] colors = {
			Color.Red,
			Color.Orange,
			Color.Purple,
			Color.Navy,
			Color.Blue,
			Color.Cyan,
			Color.Yellow,
			Color.YellowGreen,
		};

		public void assignColor(int count) {
			if (count > 0) {
				color = colors[(count - 1) % 8];
				return;
			}
			switch (units) {
			case Scope.UNITS_V:
				color = ElmBase.positiveColor;
				break;
			case Scope.UNITS_A:
				color = (CirSim.theSim.printableCheckItem.Checked) ? Color.DarkOrange : Color.Yellow;
				break;
			default:
				color = (CirSim.theSim.printableCheckItem.Checked) ? Color.Black : Color.White;
				break;
			}
		}

		void setAcCoupled(bool b) {
			if (canAcCouple()) {
				acCoupled = b;
			} else
				acCoupled = false;
		}

		bool canAcCouple() {
			return units == Scope.UNITS_V; // AC coupling is permitted if the plot is displaying volts
		}

		bool isAcCoupled() {
			return acCoupled;
		}

		public int getPlotFlags() {
			return (acCoupled ? FLAG_AC : 0);
		}
	}
}
