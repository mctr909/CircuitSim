using MainForm.Forms;

namespace MainForm {
	public class Scope {
		public const int FLAG_YELM = 32;

		// bunch of other flags go here, see dump()
		public const int FLAG_IVALUE = 2048; // Flag to indicate if IVALUE is included in dump
		public const int FLAG_PLOTS = 4096; // new-style dump with multiple plots
		public const int FLAG_PERPLOTFLAGS = 1 << 18; // new-new style dump with plot flags
		public const int FLAG_PERPLOT_MAN_SCALE = 1 << 19; // new-new style dump with manual included in each plot
		public const int FLAG_MAN_SCALE = 16;
		// other flags go here too, see dump()

		public const int VAL_POWER = 7;
		public const int VAL_POWER_OLD = 1;
		public const int VAL_VOLTAGE = 0;
		public const int VAL_CURRENT = 3;
		public const int VAL_IB = 1;
		public const int VAL_IC = 2;
		public const int VAL_IE = 3;
		public const int VAL_VBE = 4;
		public const int VAL_VBC = 5;
		public const int VAL_VCE = 6;
		public const int VAL_R = 2;
		public const int UNITS_V = 0;
		public const int UNITS_A = 1;
		public const int UNITS_W = 2;
		public const int UNITS_OHMS = 3;
		public const int UNITS_COUNT = 4;
		public static readonly double[] multa = { 2.0, 2.5, 2.0 };
		public const int V_POSITION_STEPS = 200;
		public const double MIN_MAN_SCALE = 1e-9;

		int scopePointCount = 128;
		FFT fft;
		public int position;
		// speed is sim timestep units per pixel
		public int speed;
		public int stackCount; // number of scopes in this column
		string text;
		public Rectangle rect;
		private bool bManualScale;
		bool bShowI, bShowV, bShowScale, bShowMax, bShowMin, bShowFreq;
		bool plot2d;
		bool bPlotXY;
		bool bMaxScale;

		bool bLogSpectrum;
		bool bShowFFT, showNegative, bShowRMS, bShowAverage, bShowDutyCycle;
		List<ScopePlot> plots, visiblePlots;
		int[] pixels;
		int draw_ox, draw_oy;
		float[] dpixels;
		CirSim sim;
		Bitmap imageCanvas;
		CustomGraphics imageContext;
		int alphadiv = 0;
		// scopeTimeStep to check if sim timestep has changed from previous value when
		// redrawing
		double scopeTimeStep;
		double[] scale; // Max value to scale the display to show - indexed for each value of UNITS -
						// e.g. UNITS_V, UNITS_A etc.
		bool[] reduceRange;
		double scaleX, scaleY; // for X-Y plots
		int wheelDeltaY;
		int selectedPlot;
		ScopePropertiesDialog frmProperties;
		Color curColor, voltColor;
		double gridStepX, gridStepY;
		double maxValue, minValue;
		int manDivisions = 8; // Number of vertical divisions when in manual mode
		bool drawGridLines;
		bool somethingSelected;

		public Scope(CirSim s) {
			sim = s;
			scale = new double[UNITS_COUNT];
			reduceRange = new bool[UNITS_COUNT];

			rect = new Rectangle(0, 0, 1, 1);
			allocImage();
			initialize();
		}

		void showCurrent(bool b) {
			bShowI = b;
			if (b && !showingVoltageAndMaybeCurrent())
				setValue(0);
			calcVisiblePlots();
		}

		void showVoltage(bool b) {
			bShowV = b;
			if (b && !showingVoltageAndMaybeCurrent())
				setValue(0);
			calcVisiblePlots();
		}

		void showMax(bool b) {
			bShowMax = b;
		}

		void showScale(bool b) {
			bShowScale = b;
		}

		void showMin(bool b) {
			bShowMin = b;
		}

		void showFreq(bool b) {
			bShowFreq = b;
		}

		void showFFT(bool b) {
			bShowFFT = b;
			if (!bShowFFT)
				fft = null;
		}

		void setManualScale(bool value, bool roundup) {
			if (value != bManualScale)
				clear2dView();
			bManualScale = value;
			foreach (var p in plots) {
				if (!p.manScaleSet) {
					p.manScale = getManScaleFromMaxScale(p.units, roundup);
					p.manVPosition = 0;
					p.manScaleSet = true;
				}
			}
		}

		public void resetGraph() {
			resetGraph(false);
		}

		public void resetGraph(bool full) {
			scopePointCount = 1;
			while (scopePointCount <= rect.Width)
				scopePointCount *= 2;
			if (plots == null)
				plots = new List<ScopePlot>();
			showNegative = false;
			int i;
			for (i = 0; i != plots.Count; i++)
				plots[i].reset(scopePointCount, speed, full);
			calcVisiblePlots();
			scopeTimeStep = sim.maxTimeStep;
			allocImage();
		}

		void setManualScaleValue(int plotId, double d) {
			if (plotId >= visiblePlots.Count)
				return; // Shouldn't happen, but just in case...
			clear2dView();
			visiblePlots[plotId].manScale = d;
			visiblePlots[plotId].manScaleSet = true;
		}

		double getScaleValue() {
			if (visiblePlots.Count == 0)
				return 0;
			var p = visiblePlots[0];
			return scale[p.units];
		}

		string getScaleUnitsText() {
			if (visiblePlots.Count == 0)
				return "V";
			var p = visiblePlots[0];
			return getScaleUnitsText(p.units);
		}

		static string getScaleUnitsText(int units) {
			switch (units) {
			case UNITS_A:
				return "A";
			case UNITS_OHMS:
				return "Ω";
			case UNITS_W:
				return "W";
			default:
				return "V";
			}
		}

		bool active() {
			return plots.Count > 0 && plots[0].elm != null;
		}

		void initialize() {
			resetGraph();
			scale[UNITS_W] = scale[UNITS_OHMS] = scale[UNITS_V] = 5;
			scale[UNITS_A] = .1;
			scaleX = 5;
			scaleY = .1;
			speed = 64;
			bShowMax = true;
			bShowV = bShowI = false;
			bShowScale = bShowFreq = bManualScale = bShowMin = false;
			bShowFFT = false;
			plot2d = false;
			if (!loadDefaults()) {
				// set showV and showI appropriately depending on what plots are present
				int i;
				for (i = 0; i != plots.Count; i++) {
					var plot = plots[i];
					if (plot.units == UNITS_V)
						bShowV = true;
					if (plot.units == UNITS_A)
						bShowI = true;
				}
			}
		}

		void calcVisiblePlots() {
			visiblePlots = new List<ScopePlot>();
			int i;
			int vc = 0, ac = 0, oc = 0;
			if (!plot2d) {
				for (i = 0; i != plots.Count; i++) {
					var plot = plots[i];
					if (plot.units == UNITS_V) {
						if (bShowV) {
							visiblePlots.Add(plot);
							plot.assignColor(vc++);
						}
					} else if (plot.units == UNITS_A) {
						if (bShowI) {
							visiblePlots.Add(plot);
							plot.assignColor(ac++);
						}
					} else {
						visiblePlots.Add(plot);
						plot.assignColor(oc++);
					}
				}
			} else { // In 2D mode the visible plots are the first two plots
				for (i = 0; (i < 2) && (i < plots.Count); i++) {
					visiblePlots.Add(plots[i]);
				}
			}
		}

		public void setRect(Rectangle r) {
			var w = this.rect.Width;
			this.rect = r;
			if (this.rect.Width != w)
				resetGraph();
		}

		int getWidth() {
			return rect.Width;
		}

		public int rightEdge() {
			return rect.X + rect.Width;
		}

		public void setElm(ElmBase ce) {
			plots = new List<ScopePlot>();
			if (ce is TransistorElm)
				setValue(VAL_VCE, ce);
			else
				setValue(0, ce);
			initialize();
		}

		void addElm(ElmBase ce) {
			if (ce is TransistorElm)
				addValue(VAL_VCE, ce);
			else
				addValue(0, ce);
		}

		void setValue(int val) {
			if (plots.Count > 2 || plots.Count == 0)
				return;
			var ce = plots[0].elm;
			if (plots.Count == 2 && plots[1].elm != ce)
				return;
			plot2d = bPlotXY = false;
			setValue(val, ce);
		}

		void addValue(int val, ElmBase ce) {
			if (val == 0) {
				plots.Add(new ScopePlot(ce, UNITS_V, VAL_VOLTAGE, getManScaleFromMaxScale(UNITS_V, false)));

				// create plot for current if applicable
				if (ce != null && !(ce is OutputElm ||
						ce is LogicOutputElm ||
						ce is AudioOutputElm ||
						ce is ProbeElm))
				plots.Add(new ScopePlot(ce, UNITS_A, VAL_CURRENT, getManScaleFromMaxScale(UNITS_A, false)));
			} else {
				int u = ce.getScopeUnits(val);
				plots.Add(new ScopePlot(ce, u, val, getManScaleFromMaxScale(u, false)));
				if (u == UNITS_V)
					bShowV = true;
				if (u == UNITS_A)
					bShowI = true;
			}
			calcVisiblePlots();
			resetGraph();
		}

		void setValue(int val, ElmBase ce) {
			plots = new List<ScopePlot>();
			addValue(val, ce);
			// initialize();
		}

		void setValues(int val, int ival, ElmBase ce, ElmBase yelm) {
			if (ival > 0) {
				plots = new List<ScopePlot>();
				plots.Add(new ScopePlot(ce, ce.getScopeUnits(val), val,
						getManScaleFromMaxScale(ce.getScopeUnits(val), false)));
				plots.Add(new ScopePlot(ce, ce.getScopeUnits(ival), ival,
						getManScaleFromMaxScale(ce.getScopeUnits(ival), false)));
				return;
			}
			if (yelm != null) {
				plots = new List<ScopePlot>();
				plots.Add(new ScopePlot(ce, ce.getScopeUnits(val), 0, getManScaleFromMaxScale(ce.getScopeUnits(val), false)));
				plots.Add(new ScopePlot(yelm, ce.getScopeUnits(ival), 0,
						getManScaleFromMaxScale(ce.getScopeUnits(val), false)));
				return;
			}
			setValue(val);
		}

		void setText(string s) {
			text = s;
		}

		string getText() {
			return text;
		}

		bool showingValue(int v) {
			int i;
			for (i = 0; i != plots.Count; i++) {
				var sp = plots[i];
				if (sp.value != v)
					return false;
			}
			return true;
		}

		// returns true if we have a plot of voltage and nothing else (except current).
		// The default case is a plot of voltage and current, so we're basically
		// checking if that case is true.
		bool showingVoltageAndMaybeCurrent() {
			int i;
			var gotv = false;
			for (i = 0; i != plots.Count; i++) {
				var sp = plots[i];
				if (sp.value == VAL_VOLTAGE)
					gotv = true;
				else if (sp.value != VAL_CURRENT)
					return false;
			}
			return gotv;
		}

		void combine(Scope s) {
			/*
			 * // if voltage and current are shown, remove current
			 * if (plots.size() == 2 && plots.get(0).elm == plots.get(1).elm)
			 * plots.remove(1);
			 * if (s.plots.size() == 2 && s.plots.get(0).elm == s.plots.get(1).elm)
			 * plots.add(s.plots.get(0));
			 * else
			 */
			plots = visiblePlots;
			plots.AddRange(s.visiblePlots);
			s.plots.Clear();
			calcVisiblePlots();
		}

		// separate this scope's plots into separate scopes and return them in arr[pos],
		// arr[pos+1], etc. return new length of array.
		int separate(Scope[] arr, int pos) {
			int i;
			ScopePlot lastPlot = null;
			for (i = 0; i != visiblePlots.Count; i++) {
				if (pos >= arr.Length)
					return pos;
				var s = new Scope(sim);
				var sp = visiblePlots[i];
				if (lastPlot != null && lastPlot.elm == sp.elm && lastPlot.value == VAL_VOLTAGE && sp.value == VAL_CURRENT)
					continue;
				s.setValue(sp.value, sp.elm);
				s.position = pos;
				arr[pos++] = s;
				lastPlot = sp;
				s.setFlags(getFlags());
				s.setSpeed(speed);
			}
			return pos;
		}

		void removePlot(int plot) {
			if (plot < visiblePlots.Count) {
				var p = visiblePlots[plot];
				plots.Remove(p);
				calcVisiblePlots();
			}
		}

		// called for each timestep
		public void timeStep() {
			int i;
			for (i = 0; i != plots.Count; i++)
				plots[i].timeStep();

			int x = 0;
			int y = 0;

			// For 2d plots we draw here rather than in the drawing routine
			if (plot2d && imageContext != null && plots.Count >= 2) {
				var v = plots[0].lastValue;
				var yval = plots[1].lastValue;
				if (!isManualScale()) {
					var newscale = false;
					while (v > scaleX || v < -scaleX) {
						scaleX *= 2;
						newscale = true;
					}
					while (yval > scaleY || yval < -scaleY) {
						scaleY *= 2;
						newscale = true;
					}
					if (newscale)
						clear2dView();
					var xa = v / scaleX;
					var ya = yval / scaleY;
					x = (int)(rect.Width * (1 + xa) * .499);
					y = (int)(rect.Height * (1 - ya) * .499);
				} else {
					var gridPx = calc2dGridPx(rect.Width, rect.Height);
					x = (int)(rect.Width * .499 + (v / plots[0].manScale) * gridPx
							+ gridPx * manDivisions * (double)(plots[0].manVPosition) / (double)(V_POSITION_STEPS));
					y = (int)(rect.Height * .499 - (yval / plots[1].manScale) * gridPx
							- gridPx * manDivisions * (double)(plots[1].manVPosition) / (double)(V_POSITION_STEPS));

				}
				drawTo(x, y);
			}
		}

		double calc2dGridPx(int width, int height) {
			int m = width < height ? width : height;
			return ((double)(m) / 2) / ((double)(manDivisions) / 2 + 0.05);

		}

		void drawTo(int x2, int y2) {
			if (draw_ox == -1) {
				draw_ox = x2;
				draw_oy = y2;
			}
			if (sim.printableCheckItem.Checked) {
				imageContext.setStrokeStyle(Color.Black);
			} else {
				imageContext.setStrokeStyle(Color.White);
			}
			imageContext.beginPath();
			imageContext.moveTo(draw_ox, draw_oy);
			imageContext.lineTo(x2, y2);
			imageContext.stroke();
			draw_ox = x2;
			draw_oy = y2;
		}

		void clear2dView() {
			if (imageContext != null) {
				if (sim.printableCheckItem.Checked) {
					imageContext.setFillStyle(Color.White);
				} else {
					imageContext.setFillStyle(Color.Black);
				}
				imageContext.fillRect(0, 0, rect.Width - 1, rect.Height - 1);
			}
			draw_ox = draw_oy = -1;
		}

		/*
		 * void adjustScale(double x) {
		 * scale[UNITS_V] *= x;
		 * scale[UNITS_A] *= x;
		 * scale[UNITS_OHMS] *= x;
		 * scale[UNITS_W] *= x;
		 * scaleX *= x;
		 * scaleY *= x;
		 * }
		 */

		void setMaxScale(bool s) {
			// This procedure is added to set maxscale to an explicit value instead of just
			// having a toggle
			// We call the toggle procedure first because it has useful side-effects and
			// then set the value explicitly.
			maxScale();
			bMaxScale = s;
		}

		void maxScale() {
			if (plot2d) {
				double x = 1e-8;
				scale[UNITS_V] *= x;
				scale[UNITS_A] *= x;
				scale[UNITS_OHMS] *= x;
				scale[UNITS_W] *= x;
				scaleX *= x; // For XY plots
				scaleY *= x;
				return;
			}
			// toggle max scale. This isn't on by default because, for the examples, we
			// sometimes want two plots
			// matched to the same scale so we can show one is larger. Also, for some
			// fast-moving scopes
			// (like for AM detector), the amplitude varies over time but you can't see that
			// if the scale is
			// constantly adjusting. It's also nice to set the default scale to hide noise
			// and to avoid
			// having the scale moving around a lot when a circuit starts up.
			bMaxScale = !bMaxScale;
			showNegative = false;
		}

		void drawFFTVerticalGridLines(CustomGraphics g) {
			// Draw x-grid lines and label the frequencies in the FFT that they point to.
			int prevEnd = 0;
			int divs = 20;
			var maxFrequency = 1.0 / (sim.maxTimeStep * speed * divs * 2);
			for (int i = 0; i < divs; i++) {
				int x = rect.Width * i / divs;
				if (x < prevEnd)
					continue;
				var s = ((int) Math.Round(i * maxFrequency)) + "Hz";
				var sWidth = (int) Math.Ceiling(g.measureText(s).Width);
				prevEnd = x + sWidth + 4;
				if (i > 0) {
					g.setColor(Color.DarkRed);
					g.drawLine(x, 0, x, rect.Height);
				}
				g.setColor(Color.Red);
				g.drawString(s, x + 2, rect.Height);
			}
		}

		void drawFFT(CustomGraphics g) {
			if (fft == null || fft.Size != scopePointCount)
				fft = new FFT(scopePointCount);
			var real = new double[scopePointCount];
			var imag = new double[scopePointCount];
			var plot = (visiblePlots.Count == 0) ? plots[0] : visiblePlots[0];
			var maxV = plot.maxValues;
			var minV = plot.minValues;
			int ptr = plot.ptr;
			for (int i = 0; i < scopePointCount; i++) {
				int ii = (ptr - i + scopePointCount) & (scopePointCount - 1);
				// need to average max and min or else it could cause average of function to be
				// > 0, which
				// produces spike at 0 Hz that hides rest of spectrum
				real[i] = .5 * (maxV[ii] + minV[ii]);
				imag[i] = 0;
			}
			fft.Exec(real, imag);
			double maxM = 1e-8;
			for (int i = 0; i < scopePointCount / 2; i++) {
				var m = fft.Magnitude(real[i], imag[i]);
				if (m > maxM)
					maxM = m;
			}
			int prevX = 0;
			g.setColor(Color.Red);
			if (!bLogSpectrum) {
				int prevHeight = 0;
				int y = (rect.Height - 1) - 12;
				for (int i = 0; i < scopePointCount / 2; i++) {
					int x = 2 * i * rect.Width / scopePointCount;
					// rect.width may be greater than or less than scopePointCount/2,
					// so x may be greater than or equal to prevX.
					var magnitude = fft.Magnitude(real[i], imag[i]);
					int height = (int) ((magnitude * y) / maxM);
					if (x != prevX)
						g.drawLine(prevX, y - prevHeight, x, y - height);
					prevHeight = height;
					prevX = x;
				}
			} else {
				int y0 = 5;
				int prevY = 0;
				double ymult = rect.Height / 10;
				double val0 = Math.Log(scale[plot.units]) * ymult;
				for (int i = 0; i < scopePointCount / 2; i++) {
					int x = 2 * i * rect.Width / scopePointCount;
					// rect.width may be greater than or less than scopePointCount/2,
					// so x may be greater than or equal to prevX.
					var val = Math.Log(fft.Magnitude(real[i], imag[i]));
					var y = y0 - (int) (val * ymult - val0);
					if (x != prevX)
						g.drawLine(prevX, prevY, x, y);
					prevY = y;
					prevX = x;
				}
			}
		}

		void drawSettingsWheel(CustomGraphics g) {
			const int outR = 8;
			const int inR = 5;
			const int inR45 = 4;
			const int outR45 = 6;
			if (showSettingsWheel()) {
				g.save();
				if (cursorInSettingsWheel())
					g.setColor(Color.Cyan);
				else
					g.setColor(Color.DarkGray);
				g.translate(rect.X + 18, rect.Y + rect.Height - 18);
				ElmBase.drawThickCircle(g, 0, 0, inR);
				ElmBase.drawThickLine(g, -outR, 0, -inR, 0);
				ElmBase.drawThickLine(g, outR, 0, inR, 0);
				ElmBase.drawThickLine(g, 0, -outR, 0, -inR);
				ElmBase.drawThickLine(g, 0, outR, 0, inR);
				ElmBase.drawThickLine(g, -outR45, -outR45, -inR45, -inR45);
				ElmBase.drawThickLine(g, outR45, -outR45, inR45, -inR45);
				ElmBase.drawThickLine(g, -outR45, outR45, -inR45, inR45);
				ElmBase.drawThickLine(g, outR45, outR45, inR45, inR45);
				g.restore();
			}
		}

		void draw2d(CustomGraphics g) {
			if (imageContext == null)
				return;
			g.save();
			g.translate(rect.X, rect.Y);
			g.clipRect(0, 0, rect.Width, rect.Height);

			alphadiv++;

			if (alphadiv > 2) {
				alphadiv = 0;
				imageContext.setGlobalAlpha(0.01);
				if (sim.printableCheckItem.Checked) {
					imageContext.setFillStyle(Color.White);
				} else {
					imageContext.setFillStyle(Color.Black);
				}
				imageContext.fillRect(0, 0, rect.Width, rect.Height);
				imageContext.setGlobalAlpha(1.0);
			}

			g.drawImage(imageCanvas, 0.0, 0.0);
			// g.drawImage(image, r.x, r.y, null);
			g.setColor(ElmBase.whiteColor);
			g.fillOval(draw_ox - 2, draw_oy - 2, 5, 5);
			// Axis
			g.setColor(ElmBase.positiveColor);
			g.drawLine(0, rect.Height / 2, rect.Width - 1, rect.Height / 2);
			if (!bPlotXY)
				g.setColor(Color.Yellow);
			g.drawLine(rect.Width / 2, 0, rect.Width / 2, rect.Height - 1);
			if (isManualScale()) {
				var gridPx = calc2dGridPx(rect.Width, rect.Height);
				g.setColor(Color.Gray);
				for (int i = -manDivisions; i <= manDivisions; i++) {
					if (i != 0)
						g.drawLine((int)(gridPx * i) + rect.Width / 2, 0, (int)(gridPx * i) + rect.Width / 2, rect.Height);
					g.drawLine(0, (int)(gridPx * i) + rect.Height / 2, rect.Width, (int)(gridPx * i) + rect.Height / 2);
				}
			}
			textY = 10;
			g.setColor(ElmBase.whiteColor);
			if (text != null) {
				drawInfoText(g, text);
			}
			if (bShowScale && plots.Count >= 2 && isManualScale()) {
				var px = plots[0];
				var sx = px.getUnitText(px.manScale);
				var py = plots[1];
				var sy = py.getUnitText(py.manScale);
				drawInfoText(g, "X=" + sx + "/div, Y=" + sy + "/div");
			}
			g.restore();
			drawSettingsWheel(g);
			if (!sim.dialogIsShowing() && rect.Contains(sim.mouseCursorX, sim.mouseCursorY) && plots.Count >= 2) {
				var gridPx = calc2dGridPx(rect.Width, rect.Height);
				var info = new string[2];
				var px = plots[0];
				var py = plots[1];
				double xValue;
				double yValue;
				if (isManualScale()) {
					xValue = px.manScale * ((double)(sim.mouseCursorX - rect.X - rect.Width / 2) / gridPx
							- manDivisions * px.manVPosition / (double)(V_POSITION_STEPS));
					yValue = py.manScale * ((double)(-sim.mouseCursorY + rect.Y + rect.Height / 2) / gridPx
							- manDivisions * py.manVPosition / (double)(V_POSITION_STEPS));
				} else {
					xValue = ((double)(sim.mouseCursorX - rect.X) / (0.499 * (double)(rect.Width)) - 1.0) * scaleX;
					yValue = -((double)(sim.mouseCursorY - rect.Y) / (0.499 * (double)(rect.Height)) - 1.0) * scaleY;
				}
				info[0] = px.getUnitText(xValue);
				info[1] = py.getUnitText(yValue);

				drawCrosshairsInfo(g, info, 2, true);

			}
		}

		bool showSettingsWheel() {
			return rect.Height > 100 && rect.Width > 100;
		}

		bool cursorInSettingsWheel() {
			return showSettingsWheel() &&
					sim.mouseCursorX >= rect.X &&
					sim.mouseCursorX <= rect.X + 36 &&
					sim.mouseCursorY >= rect.Y + rect.Height - 36 &&
					sim.mouseCursorY <= rect.Y + rect.Height;
		}

		public void draw(CustomGraphics g) {
			if (plots.Count == 0)
				return;

			// reset if timestep changed
			if (scopeTimeStep != sim.maxTimeStep) {
				scopeTimeStep = sim.maxTimeStep;
				resetGraph();
			}

			if (plot2d) {
				draw2d(g);
				return;
			}

			drawSettingsWheel(g);
			g.save();
			g.setColor(Color.Red);
			g.translate(rect.X, rect.Y);
			g.clipRect(0, 0, rect.Width, rect.Height);

			if (bShowFFT) {
				drawFFTVerticalGridLines(g);
				drawFFT(g);
			}

			int i;
			for (i = 0; i != UNITS_COUNT; i++) {
				reduceRange[i] = false;
				if (bMaxScale && !bManualScale)
					scale[i] = 1e-4;
			}

			int si;
			somethingSelected = false; // is one of our plots selected?

			for (si = 0; si != visiblePlots.Count; si++) {
				var plot = visiblePlots[si];
				calcPlotScale(plot);
				if (sim.scopeSelected == -1 && plot.elm != null && plot.elm.isMouseElm())
					somethingSelected = true;
				reduceRange[plot.units] = true;
			}

			checkForSelection();
			if (selectedPlot >= 0)
				somethingSelected = true;

			drawGridLines = true;
			var allPlotsSameUnits = true;
			for (i = 1; i < visiblePlots.Count; i++) {
				if (visiblePlots[i].units != visiblePlots[0].units)
					allPlotsSameUnits = false; // Don't draw horizontal grid lines unless all plots are in same units
			}

			if ((allPlotsSameUnits || bShowMax || bShowMin) && visiblePlots.Count > 0)
				calcMaxAndMin(visiblePlots[0].units);

			// draw volt plots on top (last), then current plots underneath, then everything
			// else
			for (i = 0; i != visiblePlots.Count; i++) {
				if (visiblePlots[i].units > UNITS_A && i != selectedPlot)
					drawPlot(g, visiblePlots[i], allPlotsSameUnits, false);
			}
			for (i = 0; i != visiblePlots.Count; i++) {
				if (visiblePlots[i].units == UNITS_A && i != selectedPlot)
					drawPlot(g, visiblePlots[i], allPlotsSameUnits, false);
			}
			for (i = 0; i != visiblePlots.Count; i++) {
				if (visiblePlots[i].units == UNITS_V && i != selectedPlot)
					drawPlot(g, visiblePlots[i], allPlotsSameUnits, false);
			}
			// draw selection on top. only works if selection chosen from scope
			if (selectedPlot >= 0 && selectedPlot < visiblePlots.Count)
				drawPlot(g, visiblePlots[selectedPlot], allPlotsSameUnits, true);

			if (visiblePlots.Count > 0)
				drawInfoTexts(g);

			g.restore();

			drawCrosshairs(g);

			if (plots[0].ptr > 5 && !bManualScale) {
				for (i = 0; i != UNITS_COUNT; i++)
					if (scale[i] > 1e-4 && reduceRange[i])
						scale[i] /= 2;
			}

			if ((frmProperties != null) && frmProperties.Visible)
				frmProperties.Update();
		}

		// calculate maximum and minimum values for all plots of given units
		void calcMaxAndMin(int units) {
			maxValue = -1e8;
			minValue = 1e8;
			int i;
			int si;
			for (si = 0; si != visiblePlots.Count; si++) {
				var plot = visiblePlots[si];
				if (plot.units != units)
					continue;
				var ipa = plot.startIndex(rect.Width);
				var maxV = plot.maxValues;
				var minV = plot.minValues;
				for (i = 0; i != rect.Width; i++) {
					int ip = (i + ipa) & (scopePointCount - 1);
					if (maxV[ip] > maxValue)
						maxValue = maxV[ip];
					if (minV[ip] < minValue)
						minValue = minV[ip];
				}
			}
		}

		// adjust scale of a plot
		void calcPlotScale(ScopePlot plot) {
			if (bManualScale)
				return;
			int i;
			var ipa = plot.startIndex(rect.Width);
			var maxV = plot.maxValues;
			var minV = plot.minValues;
			double max = 0;
			var gridMax = scale[plot.units];
			for (i = 0; i != rect.Width; i++) {
				int ip = (i + ipa) & (scopePointCount - 1);
				if (maxV[ip] > max)
					max = maxV[ip];
				if (minV[ip] < -max)
					max = -minV[ip];
			}
			// scale fixed at maximum?
			if (bMaxScale)
				gridMax = Math.Max(max, gridMax);
			else
				// adjust in powers of two
				while (max > gridMax)
					gridMax *= 2;
			scale[plot.units] = gridMax;
		}

		double calcGridStepX() {
			int multptr = 0;
			double gsx = 1e-15;

			var ts = sim.maxTimeStep * speed;
			while (gsx < ts * 20) {
				gsx *= multa[(multptr++) % 3];
			}
			return gsx;
		}

		double getGridMaxFromManScale(ScopePlot plot) {
			return ((double)(manDivisions) / 2 + 0.05) * plot.manScale;
		}

		void drawPlot(CustomGraphics g, ScopePlot plot, bool allPlotsSameUnits, bool selected) {
			if (plot.elm == null)
				return;
			int i;
			Color col;
			// int col = (sim.printableCheckItem.getState()) ? 0xFFFFFFFF : 0;
			// for (i = 0; i != pixels.length; i++)
			// pixels[i] = col;

			double gridMid, positionOffset;
			int multptr = 0;
			int x = 0;
			int maxy = (rect.Height - 1) / 2;

			var color = (somethingSelected) ? Color.Gray : plot.color;
			if (sim.scopeSelected == -1 && plot.elm.isMouseElm())
				color = Color.Cyan;
			else if (selected)
				color = plot.color;
			var ipa = plot.startIndex(rect.Width);
			var maxV = plot.maxValues;
			var minV = plot.minValues;
			double gridMax;

			// Calculate the max value (positive) to show and the value at the mid point of
			// the grid
			if (!isManualScale()) {
				gridMax = scale[plot.units];
				gridMid = 0;
				positionOffset = 0;
				if (allPlotsSameUnits) {
					// if we don't have overlapping scopes of different units, we can move zero
					// around.
					// Put it at the bottom if the scope is never negative.
					double mx = gridMax;
					double mn = 0;
					if (bMaxScale) {
						// scale is maxed out, so fix boundaries of scope at maximum and minimum.
						mx = maxValue;
						mn = minValue;
					} else if (showNegative || minValue < (mx + mn) * .5 - (mx - mn) * .55) {
						mn = -gridMax;
						showNegative = true;
					}
					gridMid = (mx + mn) * .5;
					gridMax = (mx - mn) * .55; // leave space at top and bottom
				}
			} else {
				gridMid = 0;
				gridMax = getGridMaxFromManScale(plot);
				positionOffset = gridMax * 2.0 * (double)(plot.manVPosition) / (double)(V_POSITION_STEPS);
			}
			plot.plotOffset = -gridMid + positionOffset;

			plot.gridMult = maxy / gridMax;

			var minRangeLo = -10 - (int) (gridMid * plot.gridMult);
			var minRangeHi = 10 - (int) (gridMid * plot.gridMult);
			if (!isManualScale()) {
				gridStepY = 1e-8;
				while (gridStepY < 20 * gridMax / maxy) {
					gridStepY *= multa[(multptr++) % 3];
				}
			} else {
				gridStepY = plot.manScale;
			}

			var minorDiv = Color.DarkGray;
			var majorDiv = Color.Gray;
			if (sim.printableCheckItem.Checked) {
				minorDiv = Color.FromArgb(0xD0, 0xD0, 0xD0);
				majorDiv = Color.FromArgb(0x80, 0x80, 0x80);
				curColor = Color.FromArgb(0xA0, 0xA0, 0x00);
			}

			// Vertical (T) gridlines
			var ts = sim.maxTimeStep * speed;
			gridStepX = calcGridStepX();

			if (drawGridLines) {
				// horizontal gridlines

				// don't show hgridlines if lines are too close together (except for center line)
				var showHGridLines = (gridStepY != 0) && (isManualScale() || allPlotsSameUnits); // Will only show
																									 // center line if we
																									 // have mixed units
				for (int ll = -100; ll <= 100; ll++) {
					if (ll != 0 && !showHGridLines)
						continue;
					var yl = maxy - (int) ((ll * gridStepY - gridMid) * plot.gridMult);
					if (yl < 0 || yl >= rect.Height - 1)
						continue;
					col = ll == 0 ? majorDiv : minorDiv;
					g.setColor(col);
					g.drawLine(0, yl, rect.Width - 1, yl);
				}

				// vertical gridlines
				double tstart = sim.t - sim.maxTimeStep * speed * rect.Width;
				double tx = sim.t - (sim.t % gridStepX);

				for (int ll = 0; ; ll++) {
					double tl = tx - gridStepX * ll;
					int gx = (int) ((tl - tstart) / ts);
					if (gx < 0)
						break;
					if (gx >= rect.Width)
						continue;
					if (tl < 0)
						continue;
					col = minorDiv;
					// first = 0;
					if (((tl + gridStepX / 4) % (gridStepX * 10)) < gridStepX) {
						col = majorDiv;
					}
					g.setColor(col);
					g.drawLine(gx, 0, gx, rect.Height - 1);
				}
			}

			// only need gridlines drawn once
			drawGridLines = false;

			g.setColor(color);

			if (isManualScale()) {
				int y0 = maxy - (int) (plot.gridMult * plot.plotOffset);
				g.drawLine(0, y0, 8, y0);
				g.drawString("0", 0, y0 - 2);
			}

			int ox = -1, oy = -1;
			for (i = 0; i != rect.Width; i++) {
				var ip = (i + ipa) & (scopePointCount - 1);
				var minvy = (int) (plot.gridMult * (minV[ip] + plot.plotOffset));
				var maxvy = (int) (plot.gridMult * (maxV[ip] + plot.plotOffset));
				if (minvy <= maxy) {
					if (minvy < minRangeLo || maxvy > minRangeHi) {
						// we got a value outside min range, so we don't need to rescale later
						reduceRange[plot.units] = false;
						minRangeLo = -1000;
						minRangeHi = 1000; // avoid triggering this test again
					}
					if (ox != -1) {
						if (minvy == oy && maxvy == oy)
							continue;
						g.drawLine(ox, maxy - oy, x + i - 1, maxy - oy);
						ox = oy = -1;
					}
					if (minvy == maxvy) {
						ox = x + i;
						oy = minvy;
						continue;
					}
					g.drawLine(x + i, maxy - minvy, x + i, maxy - maxvy - 1);
				}
			} // for (i=0...)
			if (ox != -1)
				g.drawLine(ox, maxy - oy, x + i - 1, maxy - oy); // Horizontal

		}

		// find selected plot
		void checkForSelection() {
			if (sim.dialogIsShowing())
				return;
			if (!rect.Contains(sim.mouseCursorX, sim.mouseCursorY)) {
				selectedPlot = -1;
				return;
			}
			var ipa = plots[0].startIndex(rect.Width);
			var ip = (sim.mouseCursorX - rect.X + ipa) & (scopePointCount - 1);
			var maxy = (rect.Height - 1) / 2;
			int y = maxy;
			int i;
			int bestdist = 10000;
			int best = -1;
			for (i = 0; i != visiblePlots.Count; i++) {
				var plot = visiblePlots[i];
				var maxvy = (int) (plot.gridMult * (plot.maxValues[ip] + plot.plotOffset));
				int dist = Math.Abs(sim.mouseCursorY - (rect.Y + y - maxvy));
				if (dist < bestdist) {
					bestdist = dist;
					best = i;
				}
			}
			selectedPlot = best;
		}

		void drawCrosshairs(CustomGraphics g) {
			if (sim.dialogIsShowing())
				return;
			if (!rect.Contains(sim.mouseCursorX, sim.mouseCursorY))
				return;
			if (selectedPlot < 0 && !bShowFFT)
				return;
			var info = new string[4];
			var ipa = plots[0].startIndex(rect.Width);
			var ip = (sim.mouseCursorX - rect.X + ipa) & (scopePointCount - 1);
			int ct = 0;
			var maxy = (rect.Height - 1) / 2;
			var y = maxy;
			if (selectedPlot >= 0) {
				var plot = visiblePlots[selectedPlot];
				info[ct++] = plot.getUnitText(plot.maxValues[ip]);
				var maxvy = (int) (plot.gridMult * (plot.maxValues[ip] + plot.plotOffset));
				g.setColor(plot.color);
				g.fillOval(sim.mouseCursorX - 2, rect.Y + y - maxvy - 2, 5, 5);
			}
			if (bShowFFT) {
				double maxFrequency = 1 / (sim.maxTimeStep * speed * 2);
				info[ct++] = ElmBase.getUnitText(maxFrequency * (sim.mouseCursorX - rect.X) / rect.Width, "Hz");
			}
			if (visiblePlots.Count > 0) {
				double t = sim.t - sim.maxTimeStep * speed * (rect.X + rect.Width - sim.mouseCursorX);
				info[ct++] = ElmBase.getTimeText(t);
			}
			drawCrosshairsInfo(g, info, ct, false);
		}

		void drawCrosshairsInfo(CustomGraphics g, string[] info, int ct, bool drawY) {
			int szw = 0, szh = 15 * ct;
			int i;
			for (i = 0; i != ct; i++) {
				var w = (int) g.measureText(info[i]).Width;
				if (w > szw)
					szw = w;
			}

			g.setColor(ElmBase.whiteColor);
			g.drawLine(sim.mouseCursorX, rect.Y, sim.mouseCursorX, rect.Y + rect.Height);
			if (drawY)
				g.drawLine(rect.X, sim.mouseCursorY, rect.X + rect.Width, sim.mouseCursorY);
			g.setColor(sim.printableCheckItem.Checked ? Color.White : Color.Black);
			var bx = sim.mouseCursorX;
			if (bx < szw / 2)
				bx = szw / 2;
			g.fillRect(bx - szw / 2, rect.Y - szh, szw, szh);
			g.setColor(ElmBase.whiteColor);
			for (i = 0; i != ct; i++) {
				var w = (int) g.measureText(info[i]).Width;
				g.drawString(info[i], bx - w / 2, rect.Y - 2 - (ct - 1 - i) * 15);
			}

		}

		bool canShowRMS() {
			if (visiblePlots.Count == 0)
				return false;
			var plot = visiblePlots[0];
			return (plot.units == Scope.UNITS_V || plot.units == Scope.UNITS_A);
		}

		// calc RMS and display it
		void drawRMS(CustomGraphics g) {
			if (!canShowRMS()) {
				// needed for backward compatibility
				bShowRMS = false;
				bShowAverage = true;
				drawAverage(g);
				return;
			}
			var plot = visiblePlots[0];
			int i;
			double avg = 0;
			var ipa = plot.ptr + scopePointCount - rect.Width;
			var maxV = plot.maxValues;
			var minV = plot.minValues;
			double mid = (maxValue + minValue) / 2;
			int state = -1;

			// skip zeroes
			for (i = 0; i != rect.Width; i++) {
				int ip = (i + ipa) & (scopePointCount - 1);
				if (maxV[ip] != 0) {
					if (maxV[ip] > mid)
						state = 1;
					break;
				}
			}
			int firstState = -state;
			int start = i;
			int end = 0;
			int waveCount = 0;
			double endAvg = 0;
			for (; i != rect.Width; i++) {
				int ip = (i + ipa) & (scopePointCount - 1);
				var sw = false;

				// switching polarity?
				if (state == 1) {
					if (maxV[ip] < mid)
						sw = true;
				} else if (minV[ip] > mid)
					sw = true;

				if (sw) {
					state = -state;

					// completed a full cycle?
					if (firstState == state) {
						if (waveCount == 0) {
							start = i;
							firstState = state;
							avg = 0;
						}
						waveCount++;
						end = i;
						endAvg = avg;
					}
				}
				if (waveCount > 0) {
					var m = (maxV[ip] + minV[ip]) * .5;
					avg += m * m;
				}
			}
			double rms;
			if (waveCount > 1) {
				rms = Math.Sqrt(endAvg / (end - start));
				drawInfoText(g, plot.getUnitText(rms) + "rms");
			}
		}

		void drawScale(ScopePlot plot, CustomGraphics g) {
			if (!isManualScale()) {
				if (gridStepY != 0 && (!(bShowV && bShowI))) {
					var vScaleText = " V=" + plot.getUnitText(gridStepY) + "/div";
					drawInfoText(g, "H=" + ElmBase.getUnitText(gridStepX, "s") + "/div" + vScaleText);
				}
			} else {
				if (rect.Y + rect.Height <= textY + 5)
					return;
				double x = 0;
				var hs = "H=" + ElmBase.getUnitText(gridStepX, "s") + "/div";
				g.drawString(hs, 0, textY);
				x += g.measureWidth(hs);
				const double bulletWidth = 17;
				for (int i = 0; i < visiblePlots.Count; i++) {
					var p = visiblePlots[i];
					var s = p.getUnitText(p.manScale);
					if (p != null) {
						var vScaleText = "=" + s + "/div";
						double vScaleWidth = g.measureWidth(vScaleText);
						if (x + bulletWidth + vScaleWidth > rect.Width) {
							x = 0;
							textY += 15;
							if (rect.Y + rect.Height <= textY + 5)
								return;
						}
						g.setColor(p.color);
						g.fillOval((int)x + 7, textY - 9, 8, 8);
						x += bulletWidth;
						g.setColor(ElmBase.whiteColor);
						g.drawString(vScaleText, (int)x, textY);
						x += vScaleWidth;
					}
				}
				textY += 15;
			}

		}

		void drawAverage(CustomGraphics g) {
			var plot = visiblePlots[0];
			int i;
			double avg = 0;
			var ipa = plot.ptr + scopePointCount - rect.Width;
			var maxV = plot.maxValues;
			var minV = plot.minValues;
			double mid = (maxValue + minValue) / 2;
			int state = -1;

			// skip zeroes
			for (i = 0; i != rect.Width; i++) {
				int ip = (i + ipa) & (scopePointCount - 1);
				if (maxV[ip] != 0) {
					if (maxV[ip] > mid)
						state = 1;
					break;
				}
			}
			int firstState = -state;
			int start = i;
			int end = 0;
			int waveCount = 0;
			double endAvg = 0;
			for (; i != rect.Width; i++) {
				int ip = (i + ipa) & (scopePointCount - 1);
				var sw = false;

				// switching polarity?
				if (state == 1) {
					if (maxV[ip] < mid)
						sw = true;
				} else if (minV[ip] > mid)
					sw = true;

				if (sw) {
					state = -state;

					// completed a full cycle?
					if (firstState == state) {
						if (waveCount == 0) {
							start = i;
							firstState = state;
							avg = 0;
						}
						waveCount++;
						end = i;
						endAvg = avg;
					}
				}
				if (waveCount > 0) {
					double m = (maxV[ip] + minV[ip]) * .5;
					avg += m;
				}
			}
			if (waveCount > 1) {
				avg = (endAvg / (end - start));
				drawInfoText(g, plot.getUnitText(avg) + " average");
			}
		}

		void drawDutyCycle(CustomGraphics g) {
			var plot = visiblePlots[0];
			int i;
			var ipa = plot.ptr + scopePointCount - rect.Width;
			var maxV = plot.maxValues;
			var minV = plot.minValues;
			double mid = (maxValue + minValue) / 2;
			int state = -1;

			// skip zeroes
			for (i = 0; i != rect.Width; i++) {
				int ip = (i + ipa) & (scopePointCount - 1);
				if (maxV[ip] != 0) {
					if (maxV[ip] > mid)
						state = 1;
					break;
				}
			}
			int firstState = 1;
			int start = i;
			int end = 0;
			int waveCount = 0;
			int dutyLen = 0;
			int middle = 0;
			for (; i != rect.Width; i++) {
				int ip = (i + ipa) & (scopePointCount - 1);
				var sw = false;

				// switching polarity?
				if (state == 1) {
					if (maxV[ip] < mid)
						sw = true;
				} else if (minV[ip] > mid)
					sw = true;

				if (sw) {
					state = -state;

					// completed a full cycle?
					if (firstState == state) {
						if (waveCount == 0) {
							start = end = i;
						} else {
							end = start;
							start = i;
							dutyLen = end - middle;
						}
						waveCount++;
					} else
						middle = i;
				}
			}
			if (waveCount > 1) {
				int duty = 100 * dutyLen / (end - start);
				drawInfoText(g, "Duty cycle " + duty + "%");
			}
		}

		// calc frequency if possible and display it
		void drawFrequency(CustomGraphics g) {
			// try to get frequency
			// get average
			double avg = 0;
			int i;
			var plot = visiblePlots[0];
			int ipa = plot.ptr + scopePointCount - rect.Width;
			var minV = plot.minValues;
			var maxV = plot.maxValues;
			for (i = 0; i != rect.Width; i++) {
				int ip = (i + ipa) & (scopePointCount - 1);
				avg += minV[ip] + maxV[ip];
			}
			avg /= i * 2;
			int state = 0;
			double thresh = avg * .05;
			int oi = 0;
			double avperiod = 0;
			int periodct = -1;
			double avperiod2 = 0;
			// count period lengths
			for (i = 0; i != rect.Width; i++) {
				int ip = (i + ipa) & (scopePointCount - 1);
				double q = maxV[ip] - avg;
				int os = state;
				if (q < thresh)
					state = 1;
				else if (q > -thresh)
					state = 2;
				if (state == 2 && os == 1) {
					int pd = i - oi;
					oi = i;
					// short periods can't be counted properly
					if (pd < 12)
						continue;
					// skip first period, it might be too short
					if (periodct >= 0) {
						avperiod += pd;
						avperiod2 += pd * pd;
					}
					periodct++;
				}
			}
			avperiod /= periodct;
			avperiod2 /= periodct;
			double periodstd = Math.Sqrt(avperiod2 - avperiod * avperiod);
			double freq = 1 / (avperiod * sim.maxTimeStep * speed);
			// don't show freq if standard deviation is too great
			if (periodct < 1 || periodstd > 2)
				freq = 0;
			// System.out.println(freq + " " + periodstd + " " + periodct);
			if (freq != 0)
				drawInfoText(g, ElmBase.getUnitText(freq, "Hz"));
		}

		int textY;

		void drawInfoText(CustomGraphics g, string text) {
			if (rect.Y + rect.Height <= textY + 5)
				return;
			g.drawString(text, 0, textY);
			textY += 15;
		}

		void drawInfoTexts(CustomGraphics g) {
			g.setColor(ElmBase.whiteColor);
			textY = 10;
			/*
			 * String x = position +" " + plots.size()+" ";
			 * int i;
			 * for (i = 0; i < plots.size(); i++) {
			 * x+=",";
			 * ScopePlot p = plots.get(i);
			 * // if (i > 0)
			 * x += " " + sim.locateElm(p.elm) + " " + p.value;
			 * // dump scale if units are not V or A
			 * if (p.units > UNITS_A)
			 * x += " " + scale[p.units];
			 * }
			 * drawInfoText(g, x);
			 */
			var plot = visiblePlots[0];
			if (bShowScale)
				drawScale(plot, g);
			// if (showMax || showMin)
			// calcMaxAndMin(plot.units);
			if (bShowMax)
				drawInfoText(g, "Max=" + plot.getUnitText(maxValue));
			if (bShowMin) {
				int ym = rect.Height - 5;
				g.drawString("Min=" + plot.getUnitText(minValue), 0, ym);
			}
			if (bShowRMS)
				drawRMS(g);
			if (bShowAverage)
				drawAverage(g);
			if (bShowDutyCycle)
				drawDutyCycle(g);
			var t = getScopeLabelOrText();
			if (t != null && t != "")
				drawInfoText(g, t);
			if (bShowFreq)
				drawFrequency(g);
		}

		string? getScopeText() {
			// stacked scopes? don't show text
			if (stackCount != 1)
				return null;

			// multiple elms? don't show text (unless one is selected)
			if (selectedPlot < 0 && getSingleElm() == null)
				return null;

			var plot = visiblePlots[0];
			if (selectedPlot >= 0 && visiblePlots.Count > selectedPlot)
				plot = visiblePlots[selectedPlot];
			if (plot.elm == null)
				return "";
			else
				return plot.elm.getScopeText(plot.value);
		}

		string getScopeLabelOrText() {
			var t = text;
			if (t == null) {
				t = getScopeText();
				if (t == null)
					return "";
				return t;
			} else
				return t;
		}

		void setSpeed(int sp) {
			if (sp < 1)
				sp = 1;
			if (sp > 1024)
				sp = 1024;
			speed = sp;
			resetGraph();
		}

		void properties() {
			frmProperties = new ScopePropertiesDialog(sim, this);
			CirSim.dialogShowing = frmProperties;
		}

		void speedUp() {
			if (speed > 1) {
				speed /= 2;
				resetGraph();
			}
		}

		void slowDown() {
			if (speed < 1024)
				speed *= 2;
			resetGraph();
		}

		void setPlotPosition(int plot, int v) {
			visiblePlots[plot].manVPosition = v;
		}

		// get scope element, returning null if there's more than one
		ElmBase? getSingleElm() {
			var elm = plots[0].elm;
			int i;
			for (i = 1; i < plots.Count; i++) {
				if (plots[i].elm != elm)
					return null;
			}
			return elm;
		}

		bool canMenu() {
			return (plots[0].elm != null);
		}

		bool canShowResistance() {
			var elm = getSingleElm();
			return elm != null && elm.canShowValueInScope(VAL_R);
		}

		bool isShowingVceAndIc() {
			return plot2d && plots.Count == 2 && plots[0].value == VAL_VCE && plots[1].value == VAL_IC;
		}

		int getFlags() {
			int flags = (bShowI ? 1 : 0) | (bShowV ? 2 : 0) |
				(bShowMax ? 0 : 4) | // showMax used to be always on
				(bShowFreq ? 8 : 0) |
				// In this version we always dump manual settings using the PERPLOT format
				(isManualScale() ? (FLAG_MAN_SCALE | FLAG_PERPLOT_MAN_SCALE) : 0) |
				(plot2d ? 64 : 0) |
				(bPlotXY ? 128 : 0) | (bShowMin ? 256 : 0) | (bShowScale ? 512 : 0) |
				(bShowFFT ? 1024 : 0) | (bMaxScale ? 8192 : 0) | (bShowRMS ? 16384 : 0) |
				(bShowDutyCycle ? 32768 : 0) | (bLogSpectrum ? 65536 : 0) |
				(bShowAverage ? (1 << 17) : 0);
			flags |= FLAG_PLOTS; // 4096
			int allPlotFlags = 0;
			foreach (var p in plots) {
				allPlotFlags |= p.getPlotFlags();
			}
			// If none of our plots has a flag set we will use the old format with no plot
			// flags, or
			// else we will set FLAG_PLOTFLAGS and include flags in all plots
			flags |= (allPlotFlags != 0) ? FLAG_PERPLOTFLAGS : 0; // (1<<18)
			return flags;
		}

		public string dump() {
			var vPlot = plots[0];

			var elm = vPlot.elm;
			if (elm == null)
				return null;
			var flags = getFlags();
			int eno = sim.locateElm(elm);
			if (eno < 0)
				return null;
			var x = "o " + eno + " " +
				vPlot.scopePlotSpeed + " " + vPlot.value + " "
				+ exportAsDecOrHex(flags, FLAG_PERPLOTFLAGS) + " " +
				scale[UNITS_V] + " " + scale[UNITS_A] + " " + position + " " +
				plots.Count;
			int i;
			for (i = 0; i < plots.Count; i++) {
				var p = plots[i];
				if ((flags & FLAG_PERPLOTFLAGS) != 0)
					x += " " + p.getPlotFlags().ToString("X"); // NB always export in Hex (no prefix)
				if (i > 0)
					x += " " + sim.locateElm(p.elm) + " " + p.value;
				// dump scale if units are not V or A
				if (p.units > UNITS_A)
					x += " " + scale[p.units];
				if (isManualScale()) {// In this version we always dump manual settings using the PERPLOT format
					x += " " + p.manScale + " "
							+ p.manVPosition;
				}
			}
			if (text != null)
				x += " " + CustomLogicModel.escape(text);
			return x;
		}

		public void undump(StringTokenizer st) {
			initialize();
			var e = st.nextTokenInt();
			if (e == -1)
				return;
			var ce = sim.getElm(e);
			setElm(ce);
			speed = st.nextTokenInt();
			var value = st.nextTokenInt();

			// fix old value for VAL_POWER which doesn't work for transistors (because it's
			// the same as VAL_IB)
			if (!(ce is TransistorElm) && value == VAL_POWER_OLD)
			value = VAL_POWER;

			int flags = importDecOrHex(st.nextToken());
			scale[UNITS_V] = st.nextTokenDouble();
			scale[UNITS_A] = st.nextTokenDouble();
			if (scale[UNITS_V] == 0)
				scale[UNITS_V] = .5;
			if (scale[UNITS_A] == 0)
				scale[UNITS_A] = 1;
			scaleX = scale[UNITS_V];
			scaleY = scale[UNITS_A];
			scale[UNITS_OHMS] = scale[UNITS_W] = scale[UNITS_V];
			text = null;
			var plot2dFlag = (flags & 64) != 0;
			var hasPlotFlags = (flags & FLAG_PERPLOTFLAGS) != 0;
			if ((flags & FLAG_PLOTS) != 0) {
				// new-style dump
				try {
					position = st.nextTokenInt();
					var sz = st.nextTokenInt();
					int i;
					int u = ce.getScopeUnits(value);
					if (u > UNITS_A)
						scale[u] = st.nextTokenDouble();
					setValue(value);
					// setValue(0) creates an extra plot for current, so remove that
					while (plots.Count > 1)
						plots.RemoveAt(1);

					int plotFlags = 0;
					for (i = 0; i != sz; i++) {
						if (hasPlotFlags)
							plotFlags = int.Parse("0" + st.nextToken());
						if (i != 0) {
							var ne = st.nextTokenInt();
							var val = st.nextTokenInt();
							var elm = sim.getElm(ne);
							u = elm.getScopeUnits(val);
							if (u > UNITS_A)
								scale[u] = st.nextTokenDouble();
							plots.Add(new ScopePlot(elm, u, val, getManScaleFromMaxScale(u, false)));
						}
						var p = plots[i];
						p.acCoupled = (plotFlags & ScopePlot.FLAG_AC) != 0;
						if ((flags & FLAG_PERPLOT_MAN_SCALE) != 0) {
							p.manScaleSet = true;
							p.manScale = st.nextTokenDouble();
							p.manVPosition = st.nextTokenInt();
						}
					}
					while (st.HasMoreTokens) {
						if (text == null)
							text = st.nextToken();
						else
							text += " " + st.nextToken();
					}
				} catch (Exception ee) {
				}
			} else {
				// old-style dump
				ElmBase yElm = null;
				int ivalue = 0;
				try {
					position = st.nextTokenInt();
					int ye = -1;
					if ((flags & FLAG_YELM) != 0) {
						ye = st.nextTokenInt();
						if (ye != -1)
							yElm = sim.getElm(ye);
						// sinediode.txt has yElm set to something even though there's no xy plot...?
						if (!plot2dFlag)
							yElm = null;
					}
					if ((flags & FLAG_IVALUE) != 0) {
						ivalue = st.nextTokenInt();
					}
					while (st.HasMoreTokens) {
						if (text == null)
							text = st.nextToken();
						else
							text += " " + st.nextToken();
					}
				} catch (Exception ee) {
				}
				setValues(value, ivalue, sim.getElm(e), yElm);
			}
			if (text != null)
				text = CustomLogicModel.unescape(text);
			plot2d = plot2dFlag;
			setFlags(flags);
		}

		void setFlags(int flags) {
			bShowI = (flags & 1) != 0;
			bShowV = (flags & 2) != 0;
			bShowMax = (flags & 4) == 0;
			bShowFreq = (flags & 8) != 0;
			bManualScale = (flags & FLAG_MAN_SCALE) != 0;
			bPlotXY = (flags & 128) != 0;
			bShowMin = (flags & 256) != 0;
			bShowScale = (flags & 512) != 0;
			showFFT((flags & 1024) != 0);
			bMaxScale = (flags & 8192) != 0;
			bShowRMS = (flags & 16384) != 0;
			bShowDutyCycle = (flags & 32768) != 0;
			bLogSpectrum = (flags & 65536) != 0;
			bShowAverage = (flags & (1 << 17)) != 0;
		}

		void saveAsDefault() {
			var vPlot = plots[0];
			var flags = getFlags();

			// store current scope settings as default. 1 is a version code
			Storage.setItem("scopeDefaults", "1 " + flags + " " + vPlot.scopePlotSpeed);
			Console.WriteLine("saved defaults " + flags);
		}

		bool loadDefaults() {
			var str = Storage.getItem("scopeDefaults");
			if (str == null)
				return false;
			var arr = str.Split(" ");
			var flags = int.Parse(arr[1]);
			setFlags(flags);
			speed = int.Parse(arr[2]);
			return true;
		}

		void allocImage() {
			if (null != imageCanvas) {
				imageCanvas.Dispose();
				imageCanvas = null;
			}
			imageCanvas = new Bitmap(rect.Width, rect.Height);
			imageContext = new CustomGraphics(imageCanvas);
			clear2dView();
		}

		void handleMenu(string mi, bool state) {
			if (mi == "maxscale")
				maxScale();
			if (mi == "showvoltage")
				showVoltage(state);
			if (mi == "showcurrent")
				showCurrent(state);
			if (mi == "showscale")
				showScale(state);
			if (mi == "showpeak")
				showMax(state);
			if (mi == "shownegpeak")
				showMin(state);
			if (mi == "showfreq")
				showFreq(state);
			if (mi == "showfft")
				showFFT(state);
			if (mi == "logspectrum")
				bLogSpectrum = state;
			if (mi == "showrms")
				bShowRMS = state;
			if (mi == "showaverage")
				bShowAverage = state;
			if (mi == "showduty")
				bShowDutyCycle = state;
			if (mi == "showpower")
				setValue(VAL_POWER);
			if (mi == "showib")
				setValue(VAL_IB);
			if (mi == "showic")
				setValue(VAL_IC);
			if (mi == "showie")
				setValue(VAL_IE);
			if (mi == "showvbe")
				setValue(VAL_VBE);
			if (mi == "showvbc")
				setValue(VAL_VBC);
			if (mi == "showvce")
				setValue(VAL_VCE);
			if (mi == "showvcevsic") {
				plot2d = true;
				bPlotXY = false;
				setValues(VAL_VCE, VAL_IC, getElm(), null);
				resetGraph();
			}

			if (mi == "showvvsi") {
				plot2d = state;
				bPlotXY = false;
				resetGraph();
			}
			if (mi == "manualscale")
				setManualScale(state, true);
			if (mi == "plotxy") {
				bPlotXY = plot2d = state;
				if (plot2d)
					plots = visiblePlots;
				if (plot2d && plots.Count == 1)
					selectY();
				resetGraph();
			}
			if (mi == "showresistance")
				setValue(VAL_R);
		}

		// void select() {
		// sim.setMouseElm(elm);
		// if (plotXY) {
		// sim.plotXElm = elm;
		// sim.plotYElm = yElm;
		// }
		// }

		void selectY() {
			var yElm = (plots.Count == 2) ? plots[1].elm : null;
			int e = (yElm == null) ? -1 : sim.locateElm(yElm);
			int firstE = e;
			while (true) {
				for (e++; e < sim.elmList.Count; e++) {
					var ce = sim.elmList[e];
					if ((ce is OutputElm || ce is ProbeElm) && ce != plots[0].elm) {
						yElm = ce;
						if (plots.Count == 1)
							plots.Add(new ScopePlot(yElm, UNITS_V));
						else {
							plots[1].elm = yElm;
							plots[1].units = UNITS_V;
						}
						return;
					}
				}
				if (firstE == -1)
					return;
				e = firstE = -1;
			}
			// not reached
		}

		void onMouseWheel(MouseEventArgs e) {
			wheelDeltaY += e.Delta;
			if (wheelDeltaY > 5) {
				slowDown();
				wheelDeltaY = 0;
			}
			if (wheelDeltaY < -5) {
				speedUp();
				wheelDeltaY = 0;
			}
		}

		public ElmBase getElm() {
			if (selectedPlot >= 0 && visiblePlots.Count > selectedPlot)
				return visiblePlots[selectedPlot].elm;
			return visiblePlots.Count > 0 ? visiblePlots[0].elm : plots[0].elm;
		}

		public bool viewingWire() {
			int i;
			for (i = 0; i != plots.Count; i++)
				if (plots[i].elm is WireElm)
				return true;
			return false;
		}

		ElmBase getXElm() {
			return getElm();
		}

		ElmBase? getYElm() {
			if (plots.Count == 2)
				return plots[1].elm;
			return null;
		}

		public bool needToRemove() {
			var ret = true;
			var removed = false;
			int i;
			for (i = 0; i != plots.Count; i++) {
				var plot = plots[i];
				if (sim.locateElm(plot.elm) < 0) {
					plots.RemoveAt(i--);
					removed = true;
				} else
					ret = false;
			}
			if (removed)
				calcVisiblePlots();
			return ret;
		}

		public bool isManualScale() {
			return bManualScale;
		}

		public double getManScaleFromMaxScale(int units, bool roundUp) {
			// When the user manually switches to manual scale (and we don't already have a
			// setting) then
			// call with "roundUp=true" to get a "sensible" suggestion for the scale. When
			// importing from
			// a legacy file then call with "roundUp=false" to stay as close as possible to
			// the old presentation
			double s = scale[units];
			if (units > UNITS_A)
				s = 0.5 * s;
			if (roundUp)
				return ScopePropertiesDialog.nextHighestScale((2 * s) / (double)(manDivisions));
			else
				return (2 * s) / (double)(manDivisions);
		}

		static string exportAsDecOrHex(int v, int thresh) {
			// If v>=thresh then export as hex value prefixed by "x", else export as decimal
			// Allows flags to be exported as dec if in an old value (for compatibility) or
			// in hex if new value
			if (v >= thresh)
				return "x" + v.ToString("X");
			else
				return v.ToString();
		}

		static int importDecOrHex(string s) {
			if (s.ElementAt(0) == 'x')
				return int.Parse("0" + s);
			else
				return int.Parse(s);
		}
	}
}
