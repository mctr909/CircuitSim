namespace MainForm;

internal class VoltageElm : ElmBase {
	const int FLAG_COS = 2;
	const int FLAG_PULSE_DUTY = 4;

	protected int waveform;
	protected const int WF_DC = 0;
	protected const int WF_AC = 1;
	protected const int WF_SQUARE = 2;
	protected const int WF_TRIANGLE = 3;
	protected const int WF_SAWTOOTH = 4;
	protected const int WF_PULSE = 5;
	protected const int WF_VAR = 6;

	double frequency, maxVoltage, freqTimeZero, bias,
		phaseShift, dutyCycle;

	const double defaultPulseDuty = 1 / (2 * Math.PI);

	protected VoltageElm(int xx, int yy, int wf) : base(xx, yy) {
		waveform = wf;
		maxVoltage = 5;
		frequency = 40;
		dutyCycle = .5;
		reset();
	}

	public VoltageElm(int xa, int ya, int xb, int yb, int f, StringTokenizer st) : base(xa, ya, xb, yb, f) {
		waveform = st.nextTokenInt(WF_DC);
		frequency = st.nextTokenDouble(40);
		maxVoltage = st.nextTokenDouble(5);
		bias = st.nextTokenDouble();
		phaseShift = st.nextTokenDouble();
		dutyCycle = st.nextTokenDouble(.5);
		if ((flags & FLAG_COS) != 0) {
			flags &= ~FLAG_COS;
			phaseShift = Math.PI / 2;
		}

		// old circuit files have the wrong duty cycle for pulse waveforms (wasn't
		// configurable in the past)
		if ((flags & FLAG_PULSE_DUTY) == 0 && waveform == WF_PULSE) {
			dutyCycle = defaultPulseDuty;
		}

		reset();
	}

	public override int getDumpType() {
		return 'v';
	}

	public override string dump() {
		// set flag so we know if duty cycle is correct for pulse waveforms
		if (waveform == WF_PULSE)
			flags |= FLAG_PULSE_DUTY;
		else
			flags &= ~FLAG_PULSE_DUTY;

		return base.dump() + " " + waveform + " " + frequency + " " +
				maxVoltage + " " + bias + " " + phaseShift + " " +
				dutyCycle;
		// VarRailElm adds text at the end
	}

	public override void reset() {
		freqTimeZero = 0;
		curcount = 0;
	}

	double triangleFunc(double x) {
		if (x < Math.PI)
			return x * (2 / Math.PI) - 1;
		return 1 - (x - Math.PI) * (2 / Math.PI);
	}

	int getVoltageSource() {
		return voltSource;
	}

	public override void stamp() {
		if (waveform == WF_DC)
			sim.stampVoltageSource(nodes[0], nodes[1], voltSource, getVoltage());
		else
			sim.stampVoltageSource(nodes[0], nodes[1], voltSource);
	}

	public override void doStep() {
		if (waveform != WF_DC)
			sim.updateVoltageSource(nodes[0], nodes[1], voltSource, getVoltage());
	}

	protected double getVoltage() {
		if (waveform != WF_DC && sim.dcAnalysisFlag)
			return bias;

		var w = 2 * Math.PI * (sim.t - freqTimeZero) * frequency + phaseShift;
		switch (waveform) {
		case WF_DC:
			return maxVoltage + bias;
		case WF_AC:
			return Math.Sin(w) * maxVoltage + bias;
		case WF_SQUARE:
			return bias + ((w % (2 * Math.PI) > (2 * Math.PI * dutyCycle)) ? -maxVoltage : maxVoltage);
		case WF_TRIANGLE:
			return bias + triangleFunc(w % (2 * Math.PI)) * maxVoltage;
		case WF_SAWTOOTH:
			return bias + (w % (2 * Math.PI)) * (maxVoltage / Math.PI) - maxVoltage;
		case WF_PULSE:
			return ((w % (2 * Math.PI)) < (2 * Math.PI * dutyCycle)) ? maxVoltage + bias : bias;
		default:
			return 0;
		}
	}

	protected const int circleSize = 17;

	public override void setPoints() {
		base.setPoints();
		calcLeads((waveform == WF_DC || waveform == WF_VAR) ? 8 : circleSize * 2);
	}

	public override void draw(CustomGraphics g) {
		setBbox(x, y, x2, y2);
		draw2Leads(g);
		if (waveform == WF_DC) {
			setVoltageColor(g, volts[0]);
			setPowerColor(g, false);
			interpPoint2(lead1, lead2, ref ps1, ref ps2, 0, 10);
			drawThickLine(g, ps1, ps2);
			setVoltageColor(g, volts[1]);
			setPowerColor(g, false);
			int hs = 16;
			setBbox(point1, point2, hs);
			interpPoint2(lead1, lead2, ref ps1, ref ps2, 1, hs);
			drawThickLine(g, ps1, ps2);
		} else {
			setBbox(point1, point2, circleSize);
			interpPoint(lead1, lead2, ref ps1, .5);
			drawWaveform(g, ps1);
			string inds;
			if (bias > 0 || (bias == 0 && waveform == WF_PULSE))
				inds = "+";
			else
				inds = "*";
			g.setColor(Color.White);
			g.setFont(unitsFont);
			var plusPoint = interpPoint(point1, point2, (dn / 2 + circleSize + 4) / dn, 10 * dsign);
			plusPoint.Y += 4;
			var w = (int)g.measureText(inds).Width;
			g.drawString(inds, plusPoint.X - w / 2, plusPoint.Y);
		}
		updateDotCount();
		if (sim.dragElm != this) {
			if (waveform == WF_DC)
				drawDots(g, point1, point2, curcount);
			else {
				drawDots(g, point1, lead1, curcount);
				drawDots(g, point2, lead2, -curcount);
			}
		}
		drawPosts(g);
	}

	protected void drawWaveform(CustomGraphics g, Point center) {
		g.setColor(needsHighlight() ? selectColor : Color.Gray);
		setPowerColor(g, false);
		var xc = center.X;
		var yc = center.Y;
		drawThickCircle(g, xc, yc, circleSize);
		int wl = 8;
		adjustBbox(xc - circleSize, yc - circleSize, xc + circleSize, yc + circleSize);
		int xc2;
		switch (waveform) {
		case WF_DC: {
			break;
		}
		case WF_SQUARE:
			xc2 = (int)(wl * 2 * dutyCycle - wl + xc);
			xc2 = Math.Max(xc - wl + 3, Math.Min(xc + wl - 3, xc2));
			drawThickLine(g, xc - wl, yc - wl, xc - wl, yc);
			drawThickLine(g, xc - wl, yc - wl, xc2, yc - wl);
			drawThickLine(g, xc2, yc - wl, xc2, yc + wl);
			drawThickLine(g, xc + wl, yc + wl, xc2, yc + wl);
			drawThickLine(g, xc + wl, yc, xc + wl, yc + wl);
			break;
		case WF_PULSE:
			yc += wl / 2;
			drawThickLine(g, xc - wl, yc - wl, xc - wl, yc);
			drawThickLine(g, xc - wl, yc - wl, xc - wl / 2, yc - wl);
			drawThickLine(g, xc - wl / 2, yc - wl, xc - wl / 2, yc);
			drawThickLine(g, xc - wl / 2, yc, xc + wl, yc);
			break;
		case WF_SAWTOOTH:
			drawThickLine(g, xc, yc - wl, xc - wl, yc);
			drawThickLine(g, xc, yc - wl, xc, yc + wl);
			drawThickLine(g, xc, yc + wl, xc + wl, yc);
			break;
		case WF_TRIANGLE: {
			int xl = 5;
			drawThickLine(g, xc - xl * 2, yc, xc - xl, yc - wl);
			drawThickLine(g, xc - xl, yc - wl, xc, yc);
			drawThickLine(g, xc, yc, xc + xl, yc + wl);
			drawThickLine(g, xc + xl, yc + wl, xc + xl * 2, yc);
			break;
		}
		case WF_AC: {
			int i;
			int xl = 10;
			g.beginPath();
			g.setLineWidth(3.0f);
			for (i = -xl; i <= xl; i++) {
				var yy = yc + (int)(.95 * Math.Sin(i * Math.PI / xl) * wl);
				if (i == -xl)
					g.moveTo(xc + i, yy);
				else
					g.lineTo(xc + i, yy);
			}
			g.stroke();
			g.setLineWidth(1.0f);
			break;
		}
		}
		if (sim.showValuesCheckItem.Checked) {
			var s = getShortUnitText(frequency, "Hz");
			if (dx == 0 || dy == 0)
				drawValues(g, s, circleSize);
		}
	}

	public override int getVoltageSourceCount() {
		return 1;
	}

	public override double getPower() {
		return -getVoltageDiff() * current;
	}

	public override double getVoltageDiff() {
		return volts[1] - volts[0];
	}

	public override void getInfo(string[] arr) {
		switch (waveform) {
		case WF_DC:
		case WF_VAR:
			arr[0] = "voltage source";
			break;
		case WF_AC:
			arr[0] = "A/C source";
			break;
		case WF_SQUARE:
			arr[0] = "square wave gen";
			break;
		case WF_PULSE:
			arr[0] = "pulse gen";
			break;
		case WF_SAWTOOTH:
			arr[0] = "sawtooth gen";
			break;
		case WF_TRIANGLE:
			arr[0] = "triangle gen";
			break;
		}
		arr[1] = "I = " + getCurrentText(getCurrent());
		arr[2] = (GetType().Equals(typeof(RailElm)) ? "V = " : "Vd = ") + getVoltageText(getVoltageDiff());
		int i = 3;
		if (waveform != WF_DC && waveform != WF_VAR) {
			arr[i++] = "f = " + getUnitText(frequency, "Hz");
			arr[i++] = "Vmax = " + getVoltageText(maxVoltage);
			if (waveform == WF_AC && bias == 0)
				arr[i++] = "V(rms) = " + getVoltageText(maxVoltage / 1.41421356);
			if (bias != 0)
				arr[i++] = "Voff = " + getVoltageText(bias);
			else if (frequency > 500)
				arr[i++] = "wavelength = " +
						getUnitText(2.9979e8 / frequency, "m");
		}
		if (waveform == WF_DC && current != 0 && sim.showResistanceInVoltageSources.Checked)
			arr[i++] = "(R = " + getUnitText(maxVoltage / current, "Ω") + ")";
		arr[i++] = "P = " + getUnitText(getPower(), "W");
	}

	public override EditInfo? getEditInfo(int n) {
		if (n == 0)
			return new EditInfo(waveform == WF_DC ? "Voltage" : "Max Voltage", maxVoltage, -20, 20);
		if (n == 1) {
			var ei = new EditInfo("Waveform", waveform, -1, -1);
			ei.choice = new ComboBox();
			ei.choice.Items.Add("D/C");
			ei.choice.Items.Add("A/C");
			ei.choice.Items.Add("Square Wave");
			ei.choice.Items.Add("Triangle");
			ei.choice.Items.Add("Sawtooth");
			ei.choice.Items.Add("Pulse");
			ei.choice.Items.Add("Noise");
			ei.choice.SelectedIndex = waveform;
			return ei;
		}
		if (n == 2)
			return new EditInfo("DC Offset (V)", bias, -20, 20);
		if (waveform == WF_DC)
			return null;
		if (n == 3)
			return new EditInfo("Frequency (Hz)", frequency, 4, 500);
		if (n == 4)
			return new EditInfo("Phase Offset (degrees)", phaseShift * 180 / Math.PI, -180, 180).setDimensionless();
		if (n == 5 && (waveform == WF_PULSE || waveform == WF_SQUARE))
			return new EditInfo("Duty Cycle", dutyCycle * 100, 0, 100).setDimensionless();
		return null;
	}

	public override void setEditValue(int n, EditInfo ei) {
		if (n == 0)
			maxVoltage = ei.value;
		if (n == 2)
			bias = ei.value;
		if (n == 3) {
			// adjust time zero to maintain continuity ind the waveform
			// even though the frequency has changed.
			var oldfreq = frequency;
			frequency = ei.value;
			var maxfreq = 1 / (8 * sim.maxTimeStep);
			if (frequency > maxfreq) {
				if (DialogResult.Yes == MessageBox.Show("Adjust timestep to allow for higher frequencies?", "", MessageBoxButtons.YesNo))
					sim.maxTimeStep = 1 / (32 * frequency);
				else
					frequency = maxfreq;
			}
			var adj = frequency - oldfreq;
			freqTimeZero = sim.t - oldfreq * (sim.t - freqTimeZero) / frequency;
		}
		if (n == 1) {
			var ow = waveform;
			waveform = ei.choice.SelectedIndex;
			if (waveform == WF_DC && ow != WF_DC) {
				ei.newDialog = true;
				bias = 0;
			} else if (waveform != ow)
				ei.newDialog = true;

			// change duty cycle if we're changing to or from pulse
			if (waveform == WF_PULSE && ow != WF_PULSE)
				dutyCycle = defaultPulseDuty;
			else if (ow == WF_PULSE && waveform != WF_PULSE)
				dutyCycle = .5;

			setPoints();
		}
		if (n == 4)
			phaseShift = ei.value * Math.PI / 180;
		if (n == 5)
			dutyCycle = ei.value * .01;
	}
}
